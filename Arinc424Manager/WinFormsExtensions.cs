using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Reflection;
using System.Linq.Expressions;

namespace Arinc424Manager
{
    public static class WinFormsExtensions
    {
        public static TResult InvokeEx<TControl, TResult>(this TControl control, Func<TControl, TResult> func)
        where TControl : Control
        {

            try
            {
                return control.InvokeRequired ? (TResult)control.Invoke(func, control) : func(control);
            }
            catch
            {
                return default(TResult);
            }


        }

        public static void InvokeEx<TControl>(this TControl control,
                                              Action<TControl> func)
            where TControl : Control
        {
            control.InvokeEx(c => { func(c); return c; });
        }

        public static void InvokeEx<TControl>(this TControl control, Action action)
            where TControl : Control
        {
            control.InvokeEx(c => action());
        }


    }


}
