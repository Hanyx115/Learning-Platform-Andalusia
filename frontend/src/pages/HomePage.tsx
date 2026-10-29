import './home/home.css'
import CallToAction from './home/sections/CallToAction'
import CareerPathDiscovery from './home/sections/CareerPathDiscovery'
import ComingSoon from './home/sections/ComingSoon'
import FeaturedCoursesPrograms from './home/sections/FeaturedCoursesPrograms'
import ForBusiness from './home/sections/ForBusiness'
import Hero from './home/sections/Hero'
import PartnersTrust from './home/sections/PartnersTrust'
import PopularCategories from './home/sections/PopularCategories'
import Testimonials from './home/sections/Testimonials'
import { CmsHomepageProvider } from './home/CmsHomepageContext'

function HomePage() {
  return (
    <CmsHomepageProvider>
      <Hero />
      <CareerPathDiscovery />
      <PopularCategories />
      <FeaturedCoursesPrograms />
      <ComingSoon />
      <ForBusiness />
      <PartnersTrust />
      <Testimonials />
      <CallToAction />
    </CmsHomepageProvider>
  )
}

export default HomePage
