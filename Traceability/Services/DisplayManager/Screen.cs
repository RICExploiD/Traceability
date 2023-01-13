namespace DisplayManager
{
    /// <summary>
     /// Represents a display device on a single system.
     /// </summary>
    internal sealed class Screen
    {
        /// <summary>
        /// Initializes a new instance of the Screen class.
        /// </summary>
        /// <param name="primary">A value indicating whether the display is the primary screen.</param>
        /// <param name="x">The display's top corner X value.</param>
        /// <param name="y">The display's top corner Y value.</param>
        /// <param name="w">The width of the display.</param>
        /// <param name="h">The height of the display.</param>
        internal Screen(bool primary, int x, int y, int w, int h)
        {
            this.IsPrimary = primary;
            this.TopX = x;
            this.TopY = y;
            this.Width = w;
            this.Height = h;
        }

        /// <summary>
        /// Gets a value indicating whether the display device is the primary monitor.
        /// </summary>
        internal bool IsPrimary { get; private set; }

        /// <summary>
        /// Gets the display's top corner X value.
        /// </summary>
        internal int TopX { get; private set; }

        /// <summary>
        /// Gets the display's top corner Y value.
        /// </summary>
        internal int TopY { get; private set; }

        /// <summary>
        /// Gets the width of the display.
        /// </summary>
        internal int Width { get; private set; }

        /// <summary>
        /// Gets the height of the display.
        /// </summary>
        internal int Height { get; private set; }
    }
}
