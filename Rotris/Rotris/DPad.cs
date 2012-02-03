using Microsoft.Xna.Framework.Input;

namespace Rotris
{
    public class DPad
    {
        public DPad(Keys[] key)
        {
            up = this.up;
            down = this.down;
            left = this.left;
            right = this.right;
        }

        public DPad()
        { }

        public Keys up
        {
            get;
            set;
        }
        public Keys down
        {
            get;
            set;
        }
        public Keys left
        {
            get;
            set;
        }
        public Keys right
        {
            get;
            set;
        }
    }
}
