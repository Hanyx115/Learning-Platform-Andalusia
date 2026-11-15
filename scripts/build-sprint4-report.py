from __future__ import annotations

import json
import math
import os
from collections import defaultdict
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont
from docx import Document
from docx.enum.section import WD_ORIENT, WD_SECTION
from docx.enum.style import WD_STYLE_TYPE
from docx.enum.table import WD_CELL_VERTICAL_ALIGNMENT, WD_TABLE_ALIGNMENT
from docx.enum.text import WD_ALIGN_PARAGRAPH, WD_BREAK
from docx.oxml import OxmlElement
from docx.oxml.ns import qn
from docx.shared import Cm, Inches, Pt, RGBColor


ROOT = Path(__file__).resolve().parents[1]
DATA_DIR = ROOT / "tmp" / "sprint4-report"
OUTPUT_DIR = ROOT / "output" / "sprint4-report"
ASSET_DIR = OUTPUT_DIR / "assets"
DOCX_PATH = OUTPUT_DIR / "Andalusia_Academy_Sprint_4_Final_Technical_Report.docx"

NAVY = "17324D"
DEEP_NAVY = "10283A"
COPPER = "B8785D"
TEAL = "2E8A88"
PALE_TEAL = "E8F4F3"
PALE_COPPER = "F6ECE7"
PALE_BLUE = "EAF1F7"
LIGHT = "F5F7F9"
MID = "D6DEE5"
DARK = "273642"
GREEN = "2F7D5A"
RED = "B43E47"
WHITE = "FFFFFF"
GRAY = "667784"


TABLE_DESCRIPTIONS = {
    "AspNetUsers": "Application identities and learner profile attributes used for authentication and authorization.",
    "AspNetRoles": "Named security roles, such as Learner, Instructor, Manager, and Admin.",
    "AspNetUserRoles": "Many-to-many assignment of application users to roles.",
    "AspNetUserLogins": "External login-provider identities associated with a user.",
    "AspNetUserTokens": "Provider-specific identity tokens managed by ASP.NET Core Identity.",
    "RefreshTokens": "Hashed refresh-token lifecycle records used to renew JWT access safely.",
    "CourseCategories": "Controlled classification list used to group courses.",
    "Courses": "Public course catalog records and their publication metadata.",
    "Programs": "Public multi-course program catalog records.",
    "ProgramCourses": "Ordered many-to-many membership between programs and courses.",
    "CareerPaths": "Public career-path descriptions and recommendations.",
    "CareerPathCourses": "Recommended courses linked to a career path.",
    "CareerPathPrograms": "Recommended programs linked to a career path.",
    "TrainingLocations": "Physical or online teaching locations, rooms, capacities, and map details.",
    "CourseOfferings": "Scheduled, purchasable deliveries of a course at a location and date range.",
    "ProgramOfferings": "Scheduled, purchasable deliveries of a program at a location and date range.",
    "CourseSessions": "Individual dated sessions that make up a course offering.",
    "ProgramSessions": "Optional individual dated sessions that make up a program offering.",
    "LearnerApplications": "Learner requests to join a specific course or program offering.",
    "LearnerApplicationStatusHistories": "Immutable audit trail of application status changes.",
    "Payments": "Payments associated with accepted applications and their idempotency keys.",
    "PaymentStatusHistories": "Immutable audit trail of payment state changes.",
    "Enrollments": "Active or historical learner membership in a paid course or program offering.",
    "CourseMaterials": "Published learning resources attached to a course or a specific offering.",
    "Assignments": "Course- or program-offering assessment definitions.",
    "AssignmentSubmissions": "Learner work, submission timestamps, grades, and instructor feedback.",
    "CourseSessionAttendances": "Per-learner attendance status for course sessions.",
    "ProgramSessionAttendances": "Per-learner attendance status for program sessions.",
}


TAG_PURPOSES = {
    "Applications": "Submit, review, and track learner applications.",
    "Assignments": "Update assignments, submit learner work, review submissions, and grade work.",
    "Auth": "Register, authenticate, renew, revoke, and recover user access.",
    "CareerPaths": "Browse public career-path recommendations.",
    "CourseCategories": "Browse and administer course categories.",
    "CourseMaterial": "Update an individual course material resource.",
    "CourseMaterials": "List and create course learning materials.",
    "CourseOfferingAssignments": "List and create assignments for a course offering.",
    "CourseOfferingProgress": "View learner progress for a course offering.",
    "CourseOfferingSessions": "List and create course-offering sessions.",
    "Courses": "Browse and administer course catalog records and public offerings.",
    "CourseSessionAttendance": "Read and record attendance for a course session.",
    "CourseSessions": "Reschedule, update, or cancel course sessions.",
    "Enrollments": "List a learner's enrollments or administer all enrollments.",
    "InstructorDashboard": "Provide instructor sessions, offerings, and learner rosters.",
    "LearnerMaterials": "Expose protected learning materials to enrolled learners.",
    "LearnerSchedule": "Expose the learner schedule, attendance, assignments, and progress.",
    "Payments": "Create learner payments and provide learner/admin payment views.",
    "Ping": "Provide a public API health check.",
    "Profile": "Read and update the signed-in profile or administer users by role.",
    "ProgramOfferingAssignments": "List and create assignments for a program offering.",
    "ProgramOfferingProgress": "View learner progress for a program offering.",
    "ProgramOfferingSessions": "List and create program-offering sessions.",
    "Programs": "Browse and administer program catalog records and public offerings.",
    "ProgramSessionAttendance": "Read and record attendance for a program session.",
    "ProgramSessions": "Reschedule, update, or cancel program sessions.",
    "ScheduledOfferings": "Create, view, and update scheduled course/program offerings.",
    "SchedulingConflicts": "Detect room, instructor, and duplicate-session conflicts.",
    "TrainingLocations": "Browse and administer training locations.",
}


def load_json(path: Path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


columns = [row for row in load_json(DATA_DIR / "schema-columns.json") if row["table"] != "sysdiagrams"]
foreign_keys = [row for row in load_json(DATA_DIR / "schema-fks.json") if row["from_table"] != "sysdiagrams"]
openapi = load_json(DATA_DIR / "openapi-v1.json")

table_columns: dict[str, list[dict]] = defaultdict(list)
for column in columns:
    table_columns[column["table"]].append(column)

fk_by_child_column = {(row["from_table"], row["from_column"]): row for row in foreign_keys}


def sql_type(column: dict) -> str:
    kind = column["type"]
    if kind in {"nvarchar", "nchar"}:
        length = column["max_length"]
        return f"{kind}({'max' if length == -1 else length // 2})"
    if kind in {"varchar", "char", "varbinary", "binary"}:
        length = column["max_length"]
        return f"{kind}({'max' if length == -1 else length})"
    if kind in {"decimal", "numeric"}:
        return f"{kind}({column['precision']},{column['scale']})"
    return kind


def key_label(table: str, column: dict) -> str:
    labels = []
    if column["is_primary_key"]:
        labels.append("PK")
    if (table, column["column"]) in fk_by_child_column:
        labels.append("FK")
    if column["is_unique"] and not column["is_primary_key"]:
        labels.append("UK")
    return ", ".join(labels) or "-"


def field_purpose(table: str, column: dict) -> str:
    name = column["column"]
    fk = fk_by_child_column.get((table, name))
    if fk:
        return f"Links this record to {fk['to_table']}.{fk['to_column']}; it is a foreign key to preserve referential integrity."
    if column["is_primary_key"]:
        return "Uniquely identifies the record; it is the primary key so every row has a stable database identity."
    if name == "Id":
        return "Stable identifier for the record."
    fixed = {
        "Title": "Human-readable title shown in catalog and dashboard views.",
        "Name": "Human-readable name used in user interfaces and selection lists.",
        "Description": "Long-form explanatory content shown to users.",
        "Slug": "Unique URL-safe name used by public detail routes.",
        "Status": "Current lifecycle state used to control workflow behavior and visibility.",
        "Type": "Delivery or resource classification used for filtering and behavior.",
        "CreatedAt": "UTC timestamp recording when the record was created.",
        "CreatedAtUtc": "UTC timestamp recording when the record was created.",
        "UpdatedAtUtc": "UTC timestamp recording the most recent update.",
        "StartsAtUtc": "UTC start of the scheduled offering or session.",
        "EndsAtUtc": "UTC end of the scheduled offering or session.",
        "SubmittedAt": "UTC timestamp when the learner submitted the application.",
        "SubmittedAtUtc": "UTC timestamp when the learner submitted work.",
        "ReviewedAt": "UTC timestamp when an application was reviewed.",
        "ChangedAt": "UTC timestamp of an audited status change.",
        "AttemptedAt": "UTC timestamp of a payment-provider attempt.",
        "EnrolledAt": "UTC timestamp when the enrollment became active.",
        "CompletedAt": "UTC timestamp when learning was completed; null while incomplete.",
        "CancelledAt": "UTC timestamp when an enrollment was cancelled; null otherwise.",
        "DueAtUtc": "UTC assignment deadline; null when the assignment has no deadline.",
        "GradedAtUtc": "UTC timestamp when an instructor recorded a grade.",
        "RecordedAtUtc": "UTC timestamp when attendance was recorded.",
        "IsActive": "Controls whether the record can be used without deleting its history.",
        "IsPublished": "Controls whether learners can see the learning resource.",
        "Capacity": "Maximum number of learners supported by the location or offering.",
        "PriceAmount": "Price charged for the scheduled offering.",
        "Price": "Catalog price displayed for the course or program.",
        "Amount": "Payment amount captured for the accepted application.",
        "Currency": "ISO-style currency code used with the monetary amount.",
        "SequenceOrder": "Defines the order of a course within a program.",
        "SortOrder": "Defines display order for learning materials.",
        "Score": "Numeric grade awarded to a learner submission.",
        "MaxScore": "Maximum possible score for an assignment.",
        "Feedback": "Instructor feedback returned to the learner.",
        "Content": "Learner-submitted assignment content.",
        "Note": "Optional audit or attendance note explaining the recorded state.",
        "CancellationNote": "Reason retained when a session is cancelled.",
        "ReschedulingNote": "Reason retained when a session date or location changes.",
        "RejectionReason": "Explanation retained when an application is rejected.",
        "LocationType": "Distinguishes physical rooms from online locations.",
        "BranchName": "Branch or campus name shown with the scheduled venue.",
        "RoomName": "Specific room or virtual classroom name.",
        "City": "City used for display and same-city rescheduling validation.",
        "Address": "Physical or online address details for the location.",
        "MapUrl": "Optional map link shown for a physical location.",
        "Url": "Location of the external learning resource.",
        "Provider": "Payment provider used for an attempt.",
        "ProviderReference": "Unique reference returned by the payment provider.",
        "IdempotencyKey": "Unique client key that prevents duplicate payment creation.",
        "TokenHash": "One-way hash used to validate a refresh token without storing the raw secret.",
        "ReplacedByTokenHash": "Hash linking a revoked refresh token to its rotated replacement.",
        "ExpiresAtUtc": "UTC expiry time after which the refresh token is invalid.",
        "RevokedAtUtc": "UTC revocation time; null while the refresh token remains valid.",
        "CreatedByIp": "IP address that created the refresh token for security auditing.",
        "RevokedByIp": "IP address that revoked the refresh token for security auditing.",
        "FirstName": "User's given name displayed throughout the application.",
        "LastName": "User's family name displayed throughout the application.",
        "CountryCode": "Country preference used for regional profile data.",
        "PreferredLanguage": "Language preference used by localized interfaces.",
        "ConsentedAt": "Timestamp recording when the user accepted required consent.",
        "Email": "Email address used for communication and sign-in.",
        "NormalizedEmail": "Canonical email value used for case-insensitive identity lookup.",
        "EmailConfirmed": "Indicates whether ownership of the email has been verified.",
        "UserName": "Identity username, normally aligned with the email address.",
        "NormalizedUserName": "Canonical username used for case-insensitive identity lookup.",
        "PasswordHash": "One-way password representation managed by ASP.NET Core Identity.",
        "SecurityStamp": "Changes when security-sensitive identity data changes.",
        "ConcurrencyStamp": "Optimistic-concurrency token used by ASP.NET Core Identity.",
        "PhoneNumber": "Optional user contact number.",
        "PhoneNumberConfirmed": "Indicates whether ownership of the phone number is verified.",
        "TwoFactorEnabled": "Indicates whether two-factor authentication is enabled.",
        "LockoutEnd": "Time until which sign-in is blocked; null when not locked out.",
        "LockoutEnabled": "Indicates whether lockout protection applies to the account.",
        "AccessFailedCount": "Counts failed sign-in attempts for lockout protection.",
        "LoginProvider": "External provider or token provider name.",
        "ProviderKey": "Provider-owned identifier for an external login.",
        "ProviderDisplayName": "Optional friendly name for an external login provider.",
        "Value": "Provider-specific token value managed by ASP.NET Core Identity.",
        "ConcurrencyStamp": "Optimistic-concurrency value used by Identity.",
    }
    if name in fixed:
        purpose = fixed[name]
    elif name.endswith("At") or name.endswith("AtUtc"):
        purpose = "UTC timestamp used to audit or schedule this lifecycle event."
    elif name.endswith("Hash"):
        purpose = "One-way hash used to protect a security-sensitive value."
    elif name.endswith("Confirmed") or name.startswith("Is"):
        purpose = "Boolean flag controlling the associated application behavior."
    elif name.endswith("Order"):
        purpose = "Numeric ordering value used for deterministic presentation."
    else:
        words = " ".join(part for part in _split_name(name))
        purpose = f"Stores the {words.lower()} value required by the {table} record."
    if column["is_unique"]:
        purpose += " It participates in a unique constraint that prevents duplicate key combinations."
    return purpose


def _split_name(name: str) -> list[str]:
    parts, token = [], ""
    for char in name:
        if token and char.isupper() and not token[-1].isupper():
            parts.append(token)
            token = char
        else:
            token += char
    if token:
        parts.append(token)
    return parts


def api_access(method: str, path: str, tag: str) -> str:
    key = f"{method} {path}".lower()
    if tag in {"Ping", "CareerPaths"}:
        return "Public"
    if tag in {"Courses", "Programs"} and method == "GET":
        return "Public"
    if tag == "CourseCategories" and method == "GET":
        return "Public"
    if tag == "TrainingLocations" and method == "GET" and not path.endswith("/manage"):
        return "Public"
    if tag in {"CourseOfferingSessions", "ProgramOfferingSessions", "ScheduledOfferings"} and method == "GET" and "/manage" not in path:
        return "Public"
    if tag == "Auth":
        return "Authenticated" if path.endswith("/logout") else "Public"
    if tag in {"LearnerSchedule", "LearnerMaterials"} or key == "post /api/v1/applications" or key == "post /api/v1/payments" or key.endswith("/submit"):
        return "Learner"
    if tag == "InstructorDashboard":
        return "Instructor"
    if tag in {"CourseSessionAttendance", "ProgramSessionAttendance", "CourseOfferingProgress", "ProgramOfferingProgress"}:
        return "Instructor / Manager / Admin"
    if tag in {"CourseOfferingAssignments", "ProgramOfferingAssignments"}:
        return "Instructor / Manager / Admin" if method == "GET" else "Manager / Admin"
    if tag == "Assignments" and ("/submissions" in path and method in {"GET", "PUT"}):
        return "Instructor / Manager / Admin"
    if tag == "Assignments" and method == "POST":
        return "Learner"
    if tag == "Applications":
        if path.endswith("/me"):
            return "Authenticated"
        if method == "POST" and path == "/api/v1/applications":
            return "Learner"
        return "Manager / Admin"

    if tag == "Payments":
        if path.endswith("/me"):
            return "Authenticated"
        return "Learner" if method == "POST" else "Manager / Admin"

    if tag == "Enrollments":
        if path.endswith("/me"):
            return "Authenticated"
        return "Manager / Admin"
    if tag == "Profile":
        return "Authenticated" if "{userId}" not in path and "by-role" not in path else "Manager / Admin"
    if method in {"POST", "PUT", "PATCH", "DELETE"} or "/manage" in path or tag in {"SchedulingConflicts", "CourseMaterials", "CourseMaterial", "CourseSessions", "ProgramSessions"}:
        return "Manager / Admin"
    return "Authenticated"


def schema_name(schema: dict | None) -> str:
    if not schema:
        return "-"
    if "$ref" in schema:
        return schema["$ref"].split("/")[-1]
    if schema.get("type") == "array":
        return f"array<{schema_name(schema.get('items'))}>"
    return schema.get("type", "object")


def operation_request(operation: dict) -> str:
    parts = []
    parameters = operation.get("parameters", [])
    path_params = [p["name"] for p in parameters if p.get("in") == "path"]
    query_params = [p["name"] for p in parameters if p.get("in") == "query"]
    if path_params:
        parts.append("path: " + ", ".join(path_params))
    if query_params:
        parts.append("query: " + ", ".join(query_params))
    body = operation.get("requestBody", {}).get("content", {}).get("application/json", {}).get("schema")
    if body:
        parts.append("body: " + schema_name(body))
    return "; ".join(parts) or "None"


def operation_response(operation: dict) -> str:
    preferred = []
    for status, response in operation.get("responses", {}).items():
        if str(status).startswith("2"):
            content = response.get("content", {})
            schema = None
            for media in ("application/json", "text/plain", "application/problem+json"):
                if media in content:
                    schema = content[media].get("schema")
                    break
            preferred.append(f"{status} {schema_name(schema)}")
    return ", ".join(preferred) or "Success response documented in OpenAPI"


def operation_purpose(method: str, path: str, tag: str) -> str:
    if method == "GET":
        verb = "List or retrieve"
    elif method == "POST":
        verb = "Create or execute"
    elif method == "PUT":
        verb = "Update"
    elif method == "PATCH":
        verb = "Partially update"
    else:
        verb = "Delete"
    detail = TAG_PURPOSES.get(tag, "Operate on the represented resource.")
    return f"{verb} this resource. {detail}"


def extract_operations() -> list[dict]:
    result = []
    for path, path_item in openapi["paths"].items():
        for method, operation in path_item.items():
            if method.lower() not in {"get", "post", "put", "patch", "delete"}:
                continue
            tag = (operation.get("tags") or ["Other"])[0]
            result.append({
                "tag": tag,
                "method": method.upper(),
                "path": path,
                "access": api_access(method.upper(), path, tag),
                "request": operation_request(operation),
                "response": operation_response(operation),
                "purpose": operation_purpose(method.upper(), path, tag),
            })
    return sorted(result, key=lambda item: (item["tag"], item["path"], item["method"]))


operations = extract_operations()


def font(size: int, bold: bool = False):
    names = ["arialbd.ttf" if bold else "arial.ttf", "calibrib.ttf" if bold else "calibri.ttf"]
    for name in names:
        path = Path(os.environ.get("WINDIR", "C:/Windows")) / "Fonts" / name
        if path.exists():
            return ImageFont.truetype(str(path), size)
    return ImageFont.load_default()


def wrap(draw: ImageDraw.ImageDraw, text: str, width: int, text_font) -> list[str]:
    words = text.split()
    lines, line = [], ""
    for word in words:
        trial = word if not line else f"{line} {word}"
        if draw.textbbox((0, 0), trial, font=text_font)[2] <= width:
            line = trial
        else:
            if line:
                lines.append(line)
            line = word
    if line:
        lines.append(line)
    return lines


def create_erd(title: str, tables: list[str], filename: str, cols: int = 3) -> Path:
    canvas_width = 2700
    margin = 90
    gap_x = 45
    box_width = (canvas_width - margin * 2 - gap_x * (cols - 1)) // cols
    title_height = 125
    line_height = 27
    box_header = 52
    box_pad = 18
    rows = math.ceil(len(tables) / cols)
    row_heights = []
    for row in range(rows):
        row_tables = tables[row * cols : (row + 1) * cols]
        row_heights.append(max(box_header + box_pad * 2 + line_height * len(table_columns[t]) for t in row_tables))
    canvas_height = title_height + margin + sum(row_heights) + gap_x * (rows - 1) + 100
    image = Image.new("RGB", (canvas_width, canvas_height), "white")
    draw = ImageDraw.Draw(image)
    draw.rectangle((0, 0, canvas_width, title_height), fill="#" + NAVY)
    draw.text((margin, 32), title, fill="white", font=font(40, True))
    draw.text((canvas_width - 820, 44), "PK = primary key | FK = foreign key | UK = unique", fill="#DDE8EF", font=font(22))

    positions = {}
    y = title_height + margin
    palette = [PALE_BLUE, PALE_COPPER, PALE_TEAL]
    borders = [NAVY, COPPER, TEAL]
    for row in range(rows):
        for col, table in enumerate(tables[row * cols : (row + 1) * cols]):
            x = margin + col * (box_width + gap_x)
            height = box_header + box_pad * 2 + line_height * len(table_columns[table])
            positions[table] = (x, y, x + box_width, y + height)
        y += row_heights[row] + gap_x

    for fk in foreign_keys:
        child, parent = fk["from_table"], fk["to_table"]
        if child not in positions or parent not in positions:
            continue
        cx1, cy1, cx2, cy2 = positions[child]
        px1, py1, px2, py2 = positions[parent]
        start = ((px1 + px2) // 2, py2)
        end = ((cx1 + cx2) // 2, cy1)
        mid_y = (start[1] + end[1]) // 2
        draw.line([start, (start[0], mid_y), (end[0], mid_y), end], fill="#A8B6C0", width=3)
        draw.text((start[0] + 6, start[1] + 2), "1", fill="#60717D", font=font(16, True))
        draw.text((end[0] + 6, end[1] - 24), "0..*", fill="#60717D", font=font(16, True))

    for index, table in enumerate(tables):
        x1, y1, x2, y2 = positions[table]
        fill = "#" + palette[index % len(palette)]
        border = "#" + borders[index % len(borders)]
        draw.rounded_rectangle((x1, y1, x2, y2), radius=12, fill=fill, outline=border, width=4)
        draw.rectangle((x1, y1, x2, y1 + box_header), fill=border)
        draw.text((x1 + 16, y1 + 11), table, fill="white", font=font(25, True))
        cy = y1 + box_header + box_pad
        for column in table_columns[table]:
            markers = key_label(table, column)
            marker_text = "" if markers == "-" else f" [{markers}]"
            nullable = " ?" if column["is_nullable"] else ""
            text = f"{column['column']}: {sql_type(column)}{nullable}{marker_text}"
            draw.text((x1 + 16, cy), text, fill="#" + DARK, font=font(18, bool(markers != "-")))
            cy += line_height

    footer = "Each child row references one parent unless its FK is nullable; each parent can be referenced by zero or many child rows."
    draw.text((margin, canvas_height - 55), footer, fill="#" + GRAY, font=font(19))
    path = ASSET_DIR / filename
    image.save(path, "PNG", dpi=(180, 180))
    return path


def set_cell_shading(cell, fill: str):
    tc_pr = cell._tc.get_or_add_tcPr()
    shd = tc_pr.find(qn("w:shd"))
    if shd is None:
        shd = OxmlElement("w:shd")
        tc_pr.append(shd)
    shd.set(qn("w:fill"), fill)


def set_cell_margins(cell, top=70, start=80, bottom=70, end=80):
    tc = cell._tc
    tc_pr = tc.get_or_add_tcPr()
    tc_mar = tc_pr.first_child_found_in("w:tcMar")
    if tc_mar is None:
        tc_mar = OxmlElement("w:tcMar")
        tc_pr.append(tc_mar)
    for margin_name, value in (("top", top), ("start", start), ("bottom", bottom), ("end", end)):
        node = tc_mar.find(qn(f"w:{margin_name}"))
        if node is None:
            node = OxmlElement(f"w:{margin_name}")
            tc_mar.append(node)
        node.set(qn("w:w"), str(value))
        node.set(qn("w:type"), "dxa")


def set_repeat_table_header(row):
    tr_pr = row._tr.get_or_add_trPr()
    tbl_header = OxmlElement("w:tblHeader")
    tbl_header.set(qn("w:val"), "true")
    tr_pr.append(tbl_header)


def prevent_row_split(row):
    tr_pr = row._tr.get_or_add_trPr()
    cant_split = OxmlElement("w:cantSplit")
    tr_pr.append(cant_split)


def add_page_number(paragraph):
    paragraph.alignment = WD_ALIGN_PARAGRAPH.RIGHT
    run = paragraph.add_run("Page ")
    fld_char_1 = OxmlElement("w:fldChar")
    fld_char_1.set(qn("w:fldCharType"), "begin")
    instr_text = OxmlElement("w:instrText")
    instr_text.set(qn("xml:space"), "preserve")
    instr_text.text = "PAGE"
    fld_char_2 = OxmlElement("w:fldChar")
    fld_char_2.set(qn("w:fldCharType"), "end")
    run._r.extend([fld_char_1, instr_text, fld_char_2])


def table_style(table, widths=None, font_size=7.8):
    table.alignment = WD_TABLE_ALIGNMENT.CENTER
    table.autofit = False
    for row_index, row in enumerate(table.rows):
        prevent_row_split(row)
        if row_index == 0:
            set_repeat_table_header(row)
        for index, cell in enumerate(row.cells):
            cell.vertical_alignment = WD_CELL_VERTICAL_ALIGNMENT.CENTER
            set_cell_margins(cell)
            if widths and index < len(widths):
                cell.width = widths[index]
            if row_index == 0:
                set_cell_shading(cell, NAVY)
                for paragraph in cell.paragraphs:
                    for run in paragraph.runs:
                        run.font.bold = True
                        run.font.color.rgb = RGBColor(255, 255, 255)
                        run.font.size = Pt(font_size)
            elif row_index % 2 == 0:
                set_cell_shading(cell, LIGHT)
            for paragraph in cell.paragraphs:
                paragraph.paragraph_format.space_after = Pt(0)
                paragraph.paragraph_format.space_before = Pt(0)
                for run in paragraph.runs:
                    run.font.size = Pt(font_size)
                    run.font.name = "Aptos"


def add_table(document, headers, rows, widths=None, font_size=7.8):
    table = document.add_table(rows=1, cols=len(headers))
    table.style = "Table Grid"
    for index, header in enumerate(headers):
        table.rows[0].cells[index].text = header
    for row_data in rows:
        row = table.add_row()
        for index, value in enumerate(row_data):
            row.cells[index].text = str(value)
    table_style(table, widths, font_size)
    return table


def add_callout(document, title, body, color=PALE_TEAL, accent=TEAL):
    table = document.add_table(rows=1, cols=1)
    table.autofit = False
    cell = table.cell(0, 0)
    set_cell_shading(cell, color)
    set_cell_margins(cell, 120, 160, 120, 160)
    tc_pr = cell._tc.get_or_add_tcPr()
    borders = OxmlElement("w:tcBorders")
    left = OxmlElement("w:left")
    left.set(qn("w:val"), "single")
    left.set(qn("w:sz"), "22")
    left.set(qn("w:color"), accent)
    borders.append(left)
    tc_pr.append(borders)
    paragraph = cell.paragraphs[0]
    run = paragraph.add_run(title + "\n")
    run.bold = True
    run.font.color.rgb = RGBColor.from_string(NAVY)
    run.font.size = Pt(11)
    run = paragraph.add_run(body)
    run.font.size = Pt(9.5)
    document.add_paragraph().paragraph_format.space_after = Pt(0)


def add_heading(document, text: str, level: int):
    paragraph = document.add_heading(text, level=level)
    paragraph.paragraph_format.keep_with_next = True
    return paragraph


def add_bullet(document, text: str):
    paragraph = document.add_paragraph(style="List Bullet")
    paragraph.add_run(text)
    return paragraph


def add_landscape_image_section(document, image_path: Path, caption: str):
    section = document.add_section(WD_SECTION.NEW_PAGE)
    section.orientation = WD_ORIENT.LANDSCAPE
    section.page_width, section.page_height = section.page_height, section.page_width
    section.top_margin = Cm(1.2)
    section.bottom_margin = Cm(1.2)
    section.left_margin = Cm(1.2)
    section.right_margin = Cm(1.2)
    paragraph = document.add_paragraph()
    paragraph.alignment = WD_ALIGN_PARAGRAPH.CENTER
    with Image.open(image_path) as source_image:
        ratio = source_image.width / source_image.height
    image_width_cm = min(26.8, 16.6 * ratio)
    paragraph.add_run().add_picture(str(image_path), width=Cm(image_width_cm))


def configure_document(document: Document):
    section = document.sections[0]
    section.page_width = Cm(21)
    section.page_height = Cm(29.7)
    section.top_margin = Cm(1.6)
    section.bottom_margin = Cm(1.5)
    section.left_margin = Cm(1.6)
    section.right_margin = Cm(1.6)

    normal = document.styles["Normal"]
    normal.font.name = "Aptos"
    normal.font.size = Pt(9.5)
    normal.font.color.rgb = RGBColor.from_string(DARK)
    normal.paragraph_format.space_after = Pt(5)
    normal.paragraph_format.line_spacing = 1.08

    heading_sizes = {1: 22, 2: 15, 3: 11.5}
    for level, size in heading_sizes.items():
        style = document.styles[f"Heading {level}"]
        style.font.name = "Aptos Display"
        style.font.bold = True
        style.font.size = Pt(size)
        style.font.color.rgb = RGBColor.from_string(NAVY if level < 3 else COPPER)
        style.paragraph_format.space_before = Pt(10 if level == 1 else 7)
        style.paragraph_format.space_after = Pt(5)
        style.paragraph_format.keep_with_next = True

    if "Small Note" not in [style.name for style in document.styles]:
        style = document.styles.add_style("Small Note", WD_STYLE_TYPE.PARAGRAPH)
        style.font.name = "Aptos"
        style.font.size = Pt(8)
        style.font.color.rgb = RGBColor.from_string(GRAY)

    for sec in document.sections:
        header = sec.header.paragraphs[0]
        header.text = "ANDALUSIA ACADEMY  |  SPRINT 4 FINAL TECHNICAL REPORT"
        header.style = document.styles["Small Note"]
        header.runs[0].font.bold = True
        header.runs[0].font.color.rgb = RGBColor.from_string(COPPER)
        add_page_number(sec.footer.paragraphs[0])
        sec.footer.paragraphs[0].style = document.styles["Small Note"]


def build_report():
    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    ASSET_DIR.mkdir(parents=True, exist_ok=True)

    identity_erd = create_erd(
        "ERD View 1 - Identity and Access",
        ["AspNetUsers", "AspNetRoles", "AspNetUserRoles", "AspNetUserLogins", "AspNetUserTokens", "RefreshTokens"],
        "erd-identity.png",
        cols=3,
    )
    catalog_erd = create_erd(
        "ERD View 2 - Catalog and Scheduling",
        ["CourseCategories", "Courses", "Programs", "ProgramCourses", "CareerPaths", "CareerPathCourses", "CareerPathPrograms", "TrainingLocations", "CourseOfferings", "ProgramOfferings", "CourseSessions", "ProgramSessions"],
        "erd-catalog-scheduling.png",
        cols=3,
    )
    learning_erd = create_erd(
        "ERD View 3 - Applications, Learning, and Assessment",
        ["LearnerApplications", "LearnerApplicationStatusHistories", "Payments", "PaymentStatusHistories", "Enrollments", "CourseMaterials", "Assignments", "AssignmentSubmissions", "CourseSessionAttendances", "ProgramSessionAttendances"],
        "erd-learning.png",
        cols=3,
    )

    document = Document()
    configure_document(document)

    # Cover
    document.add_paragraph("ANDALUSIA ACADEMY").runs[0].font.color.rgb = RGBColor.from_string(COPPER)
    document.add_paragraph().add_run("SPRINT 4").font.size = Pt(18)
    title = document.add_paragraph()
    title.paragraph_format.space_before = Pt(18)
    run = title.add_run("Final Technical Report")
    run.font.name = "Aptos Display"
    run.font.size = Pt(34)
    run.font.bold = True
    run.font.color.rgb = RGBColor.from_string(NAVY)
    subtitle = document.add_paragraph("Verification, final ERD, database dictionary, and API documentation")
    subtitle.runs[0].font.size = Pt(16)
    subtitle.runs[0].font.color.rgb = RGBColor.from_string(TEAL)
    document.add_paragraph("Prepared from the verified develop codebase and live local OpenAPI/database metadata\n22 October 2026 | Africa/Cairo")
    document.add_paragraph().add_run().add_break(WD_BREAK.PAGE)

    add_heading(document, "Executive conclusion", 1)
    add_callout(
        document,
        "VERIFIED RESULT: PASS",
        "The isolated integration-test connection is fixed. The complete backend and frontend automated suite passed, the live API Sprint 4 workflow passed 13 end-to-end checks, and browser verification passed for learner, instructor, and administrator experiences.",
        PALE_TEAL,
        GREEN,
    )
    metrics = [
        ("Backend tests", "160 passed"),
        ("Frontend tests", "30 passed"),
        ("API operations", f"{len(operations)} documented"),
        ("Application tables", f"{len(table_columns)} documented"),
        ("Database fields", f"{len(columns)} documented"),
        ("Foreign keys", f"{len(foreign_keys)} documented"),
    ]
    add_table(document, ["Evidence", "Result"], metrics, [Cm(6), Cm(11.5)], 9)
    document.add_paragraph(
        "The test runner now provides a dedicated ANDALUSIA_TEST_CONNECTION_STRING, targets the isolated AndalusiaAcademyIntegrationTests database, applies migrations at startup, and disables integration-test parallelization so tests do not race over shared SQL state."
    )

    add_heading(document, "Contents", 2)
    for item in [
        "1. Sprint 4 scope and traceability",
        "2. Verification evidence",
        "3. Architecture and authorization",
        "4. Final entity-relationship model",
        "5. Complete database dictionary",
        "6. Complete API reference",
        "7. Manual workflow evidence",
        "8. Known non-blocking items and recommendations",
    ]:
        add_bullet(document, item)

    document.add_page_break()
    add_heading(document, "1. Sprint 4 scope and traceability", 1)
    document.add_paragraph(
        "The official Phase 1 MVB plan defines Sprint 4 as the first complete role-based learning-management increment. The table below maps that scope to verified implementation evidence."
    )
    trace_rows = [
        ("Learner dashboard", "Profile, applications, enrollments, and schedule pages", "Pass"),
        ("My Courses / Programs", "Authenticated enrollment list and learning workspace", "Pass"),
        ("Learning page", "Schedule, location, materials, assignments, attendance, and progress", "Pass"),
        ("Instructor dashboard", "Assigned sessions, offerings, and enrolled students", "Pass"),
        ("Attendance and assignments", "Attendance records, submission, grading, and feedback APIs/UI", "Pass"),
        ("Administrator/manager dashboard", "Catalog, locations, learners, instructors, offerings, sessions, and payments", "Pass"),
        ("API roles and permissions", "JWT bearer policies for Learner, Instructor, Manager, and Admin", "Pass"),
        ("Sprint 4 submission artifacts", "This ERD and API documentation report", "Pass"),
    ]
    add_table(document, ["Plan item", "Implemented evidence", "Status"], trace_rows, [Cm(4.4), Cm(10.6), Cm(2.4)], 8.5)

    add_heading(document, "2. Verification evidence", 1)
    add_heading(document, "2.1 Automated suite", 2)
    auto_rows = [
        ("Solution restore", "Completed", "NuGet audit feed unavailable warning only"),
        ("Release build", "Completed with 0 errors", "6 NU1900 audit-feed warnings"),
        ("Domain unit tests", "1 passed", "No failures"),
        ("Application unit tests", "26 passed", "No failures"),
        ("API integration tests", "133 passed", "Uses isolated SQL database"),
        ("Backend total", "160 passed", "No failures"),
        ("Frontend lint", "Passed", "No errors"),
        ("Frontend unit/component tests", "30 passed in 11 files", "No failures"),
        ("Frontend production build", "Passed", "One 506.12 kB chunk warning"),
    ]
    add_table(document, ["Check", "Result", "Notes"], auto_rows, [Cm(5), Cm(4.5), Cm(7.8)], 8.5)

    add_heading(document, "2.2 Live API smoke workflow", 2)
    document.add_paragraph(
        "A disposable local learner, instructor, and administrator were used to verify the complete Sprint 4 business flow against the running Docker API and SQL Server. All 13 assertions passed."
    )
    for check in [
        "Created role-specific accounts and verified authentication.",
        "Created an active physical location, scheduled course offering, and instructor-led session.",
        "Published protected learning material and an assignment.",
        "Submitted and accepted a learner application.",
        "Completed a simulated payment and confirmed enrollment creation.",
        "Submitted learner work, recorded attendance, graded the submission, and verified progress.",
        "Verified learner schedule, materials, assignments, attendance, and progress endpoints.",
        "Verified instructor sessions, offerings, and student roster endpoints.",
        "Verified administrator enrollment and payment views.",
        "Verified that a learner receives HTTP 403 when attempting to create a location.",
    ]:
        add_bullet(document, check)

    add_heading(document, "2.3 Browser verification", 2)
    browser_rows = [
        ("Learner", "Profile, active enrollment, learning page, scheduled session, protected materials, graded assignment, progress"),
        ("Instructor", "Assigned sessions, course offering, learner roster, attendance entry route"),
        ("Administrator", "Catalog, locations, learners, instructors, payments"),
    ]
    add_table(document, ["Role", "Verified UI surface"], browser_rows, [Cm(3.5), Cm(14)], 8.8)
    add_callout(document, "Local origin note", "The frontend must be opened as http://localhost:5173 when the API CORS allow-list contains localhost. Using http://127.0.0.1:5173 is a different browser origin and is correctly rejected unless explicitly added.", PALE_COPPER, COPPER)

    add_heading(document, "3. Architecture and authorization", 1)
    document.add_paragraph(
        "The solution uses a React/Vite frontend, an ASP.NET Core versioned REST API, layered Application/Domain/Infrastructure projects, Entity Framework Core, SQL Server, ASP.NET Core Identity, and JWT bearer authentication. Docker Compose supplies reproducible local API and database services; the browser frontend calls the API over the configured CORS origin."
    )
    auth_rows = [
        ("Public", "Catalog, career paths, public offering/session details, location list, authentication"),
        ("Learner", "Applications, payments, personal enrollments, protected materials, schedule, attendance, assignments, progress"),
        ("Instructor", "Assigned sessions, offerings, learners, attendance, submissions, grading, progress"),
        ("Manager", "Catalog, locations, offerings, sessions, conflicts, applications, enrollments, payments, learning operations"),
        ("Admin", "Manager capabilities plus administrative people and system oversight"),
    ]
    add_table(document, ["Access level", "Primary capability"], auth_rows, [Cm(3.5), Cm(14)], 8.7)

    document.add_page_break()
    add_heading(document, "4. Final entity-relationship model", 1)
    document.add_paragraph(
        f"The physical SQL Server model contains {len(table_columns)} application tables, {len(columns)} application fields, and {len(foreign_keys)} foreign-key relationships. It is shown in three domain views for readability. Cross-domain references are fully enumerated in the relationship matrix after the diagrams."
    )
    add_callout(document, "Application schema boundary", "The SQL Server-owned sysdiagrams helper table is intentionally excluded because it stores diagram-tool metadata, not Andalusia Academy business data.", PALE_BLUE, NAVY)
    add_landscape_image_section(document, identity_erd, "Figure 1. Identity and access tables.")
    add_landscape_image_section(document, catalog_erd, "Figure 2. Catalog, offerings, locations, and sessions.")
    add_landscape_image_section(document, learning_erd, "Figure 3. Applications, payments, enrollment, materials, assignments, and attendance.")

    portrait_section = document.add_section(WD_SECTION.NEW_PAGE)
    portrait_section.orientation = WD_ORIENT.PORTRAIT
    portrait_section.page_width = Cm(21)
    portrait_section.page_height = Cm(29.7)
    portrait_section.top_margin = Cm(1.6)
    portrait_section.bottom_margin = Cm(1.5)
    portrait_section.left_margin = Cm(1.6)
    portrait_section.right_margin = Cm(1.6)

    add_heading(document, "4.1 Complete relationship matrix", 2)
    relationship_rows = []
    for fk in foreign_keys:
        child_column = next(c for c in table_columns[fk["from_table"]] if c["column"] == fk["from_column"])
        required = "Required" if not child_column["is_nullable"] else "Optional"
        relationship_rows.append((
            f"{fk['from_table']}.{fk['from_column']}",
            f"{fk['to_table']}.{fk['to_column']}",
            required,
            "Parent 1 -> child 0..many",
        ))
    add_table(document, ["Child foreign key", "Parent key", "Child reference", "Cardinality"], relationship_rows, [Cm(5.2), Cm(5.2), Cm(2.8), Cm(4.4)], 7.4)

    document.add_page_break()
    add_heading(document, "5. Complete database dictionary", 1)
    document.add_paragraph(
        "Every application table and every physical field is listed below. PK indicates a primary-key member, FK a foreign key, and UK a unique-key member. Composite keys are correctly shown by PK on each participating column."
    )
    for table_name in sorted(table_columns):
        add_heading(document, table_name, 2)
        document.add_paragraph(TABLE_DESCRIPTIONS.get(table_name, "Application data table."))
        rows = []
        for column in table_columns[table_name]:
            rows.append((
                column["column"],
                sql_type(column),
                key_label(table_name, column),
                "Yes" if column["is_nullable"] else "No",
                field_purpose(table_name, column),
            ))
        add_table(document, ["Field", "SQL type", "Key", "Nullable", "Why it exists / key rationale"], rows, [Cm(3.2), Cm(2.8), Cm(1.5), Cm(1.5), Cm(8.6)], 7.2)
        document.add_paragraph()

    document.add_page_break()
    add_heading(document, "6. Complete API reference", 1)
    document.add_paragraph(
        "Development base URL: http://localhost:8080. Swagger UI: /swagger/index.html. OpenAPI document: /openapi/v1.json. Protected endpoints require an Authorization header in the form 'Bearer <access-token>'. JSON is the primary request and response format; validation and authorization failures use standard HTTP problem details."
    )
    add_callout(document, "Versioning", "All application routes are documented under API version v1. The route casing shown below matches the generated OpenAPI document; clients should use it exactly as documented.", PALE_BLUE, NAVY)
    by_tag: dict[str, list[dict]] = defaultdict(list)
    for operation in operations:
        by_tag[operation["tag"]].append(operation)
    for tag in sorted(by_tag):
        add_heading(document, tag, 2)
        document.add_paragraph(TAG_PURPOSES.get(tag, "Operations for this API resource."))
        rows = [
            (op["method"], op["path"], op["access"], op["request"], op["response"], op["purpose"])
            for op in by_tag[tag]
        ]
        add_table(
            document,
            ["Method", "Route", "Access", "Request", "Success", "Purpose"],
            rows,
            [Cm(1.5), Cm(5.1), Cm(2.6), Cm(3.2), Cm(2.2), Cm(3.0)],
            6.7,
        )
        document.add_paragraph()

    document.add_page_break()
    add_heading(document, "7. Manual workflow evidence", 1)
    workflow_rows = [
        ("1", "Administrator", "Create an active location with capacity and city metadata."),
        ("2", "Administrator", "Create a course offering inside the course date range and assign an instructor."),
        ("3", "Administrator", "Create a non-overlapping session inside the offering period."),
        ("4", "Administrator", "Publish course material and create an assignment."),
        ("5", "Learner", "Submit an application for the scheduled offering."),
        ("6", "Administrator", "Accept the application and confirm its status history."),
        ("7", "Learner", "Create the simulated payment and confirm enrollment."),
        ("8", "Learner", "Open the learning workspace and submit assignment content."),
        ("9", "Instructor", "Record attendance and grade the learner submission."),
        ("10", "Learner", "Verify schedule, materials, assignment result, attendance, and progress."),
        ("11", "Administrator", "Verify all-enrollment and all-payment management views."),
        ("12", "Security", "Verify a learner cannot create a training location (HTTP 403)."),
    ]
    add_table(document, ["Step", "Role", "Expected verified behavior"], workflow_rows, [Cm(1.3), Cm(3.1), Cm(13)], 8.5)
    document.add_paragraph(
        "Live verification identifiers: enrollment da0d1f5b-060b-4d5e-be06-26c700547441; offering 97955582-193d-4ee8-9b89-b1d5b08257b3; session 9c81ee8e-dd14-4aa7-b3f2-a76dec276456; assignment 8dcce2fd-648e-449e-8b06-e164babe1f82. These identifiers belong to disposable local demonstration data."
    )

    add_heading(document, "8. Known non-blocking items and recommendations", 1)
    known_rows = [
        ("NuGet NU1900 warnings", "The vulnerability-audit feed was unavailable during the run; restore/build/test still passed. Re-run audit when network access is available."),
        ("Frontend bundle size", "The main production chunk is 506.12 kB, slightly above Vite's 500 kB advisory threshold. Consider route-based code splitting."),
        ("Local CORS origin", "Use http://localhost:5173, or explicitly add any alternate development origin such as http://127.0.0.1:5173."),
        ("Development database hygiene", "The shared development database contains historical test rows. The automated integration suite now uses an isolated database; optionally refresh the development volume before a clean demonstration."),
        ("Security package warning", "The previously noted System.Security.Cryptography.Pkcs package update remains intentionally deferred to a dedicated security-maintenance change."),
    ]
    add_table(document, ["Item", "Recommendation"], known_rows, [Cm(4.5), Cm(13)], 8.4)
    add_callout(document, "Final assessment", "Sprint 4 is functionally demonstrated across learner, instructor, manager/admin, API, database, and browser layers. The listed items are improvements, not release-blocking test failures.", PALE_TEAL, GREEN)

    document.add_paragraph()
    end = document.add_paragraph("END OF REPORT")
    end.alignment = WD_ALIGN_PARAGRAPH.CENTER
    end.runs[0].font.bold = True
    end.runs[0].font.color.rgb = RGBColor.from_string(COPPER)

    # Apply headers and footers to sections created after initial setup.
    for sec in document.sections:
        header = sec.header.paragraphs[0]
        if not header.text:
            header.text = "ANDALUSIA ACADEMY  |  SPRINT 4 FINAL TECHNICAL REPORT"
        header.style = document.styles["Small Note"]
        for run in header.runs:
            run.font.bold = True
            run.font.color.rgb = RGBColor.from_string(COPPER)
        footer = sec.footer.paragraphs[0]
        if not footer.text:
            add_page_number(footer)
        footer.style = document.styles["Small Note"]

    document.save(DOCX_PATH)
    print(DOCX_PATH)


if __name__ == "__main__":
    build_report()
