using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;

namespace ToolRackSetup
{
    public static class MouseUtilities
    {
        public static Point CorrectGetPosition(Visual relativeTo)
        {
            Win32Point w32Mouse = new Win32Point();
            GetCursorPos(ref w32Mouse);
            return relativeTo.PointFromScreen(new Point(w32Mouse.X, w32Mouse.Y));
        }

        public static Point GetMousePos()
        {
            Win32Point wp = new Win32Point();
            GetCursorPos(ref wp);
            return new Point(wp.X, wp.Y);
        }

        [StructLayout(LayoutKind.Sequential)]
        
        internal struct Win32Point
        {
            public Int32 X;
            public Int32 Y;
        };


        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetCursorPos(ref Win32Point pt);
    }
}