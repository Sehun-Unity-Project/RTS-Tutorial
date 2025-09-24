using System.Xml.Serialization;

namespace GameDevTV.RTS.Units
{
    public interface ISelectable
    {
        public void Select();
        public void DeSelect();
    }
}