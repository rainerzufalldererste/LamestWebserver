using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LamestWebserver.JScriptBuilder
{
    public static class JSAnimation
    {
        public enum JSAnimationType
        {
            Linear, Differencial, Quadreatic
        }

        public enum JSAxis
        {
            X, Y, Both
        }

        public static JSFunction ExpandElementToSize(IJSValue element, JSAxis axis = JSAxis.Y, JSAnimationType animationType = JSAnimationType.Differencial, int speedFactor = 10)
        {
            throw new NotImplementedException();
        }

        public static JSFunction DecreaseElementToZero(IJSValue element, JSAxis axis = JSAxis.Y, JSAnimationType animationType = JSAnimationType.Differencial, int speedFactor = 10)
        {
            throw new NotImplementedException();
        }

        public static JSFunction DecreaseElementToSize(IJSValue element, JSAxis axis = JSAxis.Y, JSAnimationType animationType = JSAnimationType.Differencial, int speedFactor = 10)
        {
            throw new NotImplementedException();
        }
    }
}
