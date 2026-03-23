using BusinessLayer.Abstract;
using EntityLayer.Concrete;

namespace BusinessLayer.Concrete
{
    public class SliderManager : GenericManager<Slider>, ISliderService
    {
        public SliderManager(IGenericDal<Slider> SliderDal) : base(SliderDal)
        {
        }
    }
}
