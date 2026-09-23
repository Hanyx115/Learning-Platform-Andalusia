import { arTranslations } from './locales/ar'
import { enTranslations } from './locales/en'

describe('academy brand copy', () => {
  it('uses the approved Arabic platform name', () => {
    expect(arTranslations.app.name).toBe('أكاديمية الأندلسية')
    expect(arTranslations.footer.copyright).toContain('أكاديمية الأندلسية')
  })

  it('describes the academy without presenting bilingual support as the product message', () => {
    expect(JSON.stringify(enTranslations)).not.toMatch(/bilingual/i)
    expect(JSON.stringify(arTranslations)).not.toContain('ثنائي')
  })
})
