using System.Drawing;
using System.Drawing.Imaging;

namespace Remote.Agent.Screen;

public sealed class ScreenCapture
{
    public byte[] Capture()
    {
        var bounds = System.Windows.Forms.Screen.PrimaryScreen!.Bounds;

        using var bitmap = new Bitmap(
            bounds.Width,
            bounds.Height,
            PixelFormat.Format32bppArgb
        );

        using var graphics =
            Graphics.FromImage(bitmap);

        graphics.CopyFromScreen(
            bounds.Left,
            bounds.Top,
            0,
            0,
            bounds.Size,
            CopyPixelOperation.SourceCopy
        );

        using var stream = new MemoryStream();

        bitmap.Save(
            stream,
            ImageFormat.Jpeg
        );

        return stream.ToArray();
    }
}