using ScreenTest;
using System.Drawing;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Tesseract;
using ImageFormat = System.Drawing.Imaging.ImageFormat;

/**
 * Created by Kishore Kumar S - Dated on 17/07/2023.
 */

namespace ScreenTest
{
    public class PrintScreen
    {
        /// <summary>
        /// Creates an Image object containing a screen shot of the entire desktop
        /// </summary>
        /// <returns></returns>
        public Image CaptureScreen()
        {
            return CaptureWindow(User32.GetDesktopWindow());
        }

        /// <summary>
        /// Creates an Image object containing a screen shot of a specific window
        /// </summary>
        /// <param name="handle">The handle to the window. (In windows forms, this is obtained by the Handle property)</param>
        /// <returns></returns>
        public Image CaptureWindow(IntPtr handle)
        {
            // get te hDC of the target window
            IntPtr hdcSrc = User32.GetWindowDC(handle);
            // get the size
            User32.RECT windowRect = new User32.RECT();
            User32.GetWindowRect(handle, ref windowRect);
            int width = windowRect.right - windowRect.left;
            int height = windowRect.bottom - windowRect.top;
            // create a device context we can copy to
            IntPtr hdcDest = GDI32.CreateCompatibleDC(hdcSrc);
            // create a bitmap we can copy it to,
            // using GetDeviceCaps to get the width/height
            IntPtr hBitmap = GDI32.CreateCompatibleBitmap(hdcSrc, width, height);
            // select the bitmap object
            IntPtr hOld = GDI32.SelectObject(hdcDest, hBitmap);
            // bitblt over
            GDI32.BitBlt(hdcDest, 0, 0, width, height, hdcSrc, 0, 0, GDI32.SRCCOPY);
            // restore selection
            GDI32.SelectObject(hdcDest, hOld);
            // clean up
            GDI32.DeleteDC(hdcDest);
            User32.ReleaseDC(handle, hdcSrc);

            // get a .NET image object for it
            Image img = Image.FromHbitmap(hBitmap);
            // free up the Bitmap object
            GDI32.DeleteObject(hBitmap);

            return img;
        }

        /// <summary>
        /// Captures a screen shot of a specific window, and saves it to a file
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="filename"></param>
        /// <param name="format"></param>
        public void CaptureWindowToFile(IntPtr handle, string filename, ImageFormat format)
        {
            Image img = CaptureWindow(handle);
            img.Save(filename, format);
        }

        /// <summary>
        /// Captures a screen shot of the entire desktop, and saves it to a file
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="format"></param>
        public void CaptureScreenToFile(string filename, ImageFormat format)
        {
            Image img = CaptureScreen();
            img.Save(filename, format);
        }
        private class GDI32
        {

            public const int SRCCOPY = 0x00CC0020; // BitBlt dwRop parameter

            [DllImport("gdi32.dll")]
            public static extern bool BitBlt(IntPtr hObject, int nXDest, int nYDest,
                int nWidth, int nHeight, IntPtr hObjectSource,
                int nXSrc, int nYSrc, int dwRop);
            [DllImport("gdi32.dll")]
            public static extern IntPtr CreateCompatibleBitmap(IntPtr hDC, int nWidth,
                int nHeight);
            [DllImport("gdi32.dll")]
            public static extern IntPtr CreateCompatibleDC(IntPtr hDC);
            [DllImport("gdi32.dll")]
            public static extern bool DeleteDC(IntPtr hDC);
            [DllImport("gdi32.dll")]
            public static extern bool DeleteObject(IntPtr hObject);
            [DllImport("gdi32.dll")]
            public static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);
        }
        private class User32
        {
            [StructLayout(LayoutKind.Sequential)]
            public struct RECT
            {
                public int left;
                public int top;
                public int right;
                public int bottom;
            }

            [DllImport("user32.dll")]
            public static extern IntPtr GetDesktopWindow();
            [DllImport("user32.dll")]
            public static extern IntPtr GetWindowDC(IntPtr hWnd);
            [DllImport("user32.dll")]
            public static extern IntPtr ReleaseDC(IntPtr hWnd, IntPtr hDC);
            [DllImport("user32.dll")]
            public static extern IntPtr GetWindowRect(IntPtr hWnd, ref RECT rect);

        }
    }
}

namespace Screentest
{
    class Program
    {
        static void Main()
        {
            MainAsync();
        }

        static async void MainAsync()
        {
            DirectoryInfo di = new DirectoryInfo(@"C:\Users\DELL");
            if (!di.Exists) { di.Create(); }


            while (true)
            {
                Thread.Sleep(5000);
                PrintScreen ps = new PrintScreen();
                ps.CaptureScreenToFile(di + $"\\screenShootImg.png", ImageFormat.Png);
                Console.WriteLine("No.1 Completed");
                var path = @"tessdata";
                var sourceFilePath = di + $"\\screenShootImg.png";
                using (var engine = new TesseractEngine(path, "eng"))
                {
                    engine.SetVariable("user_defined_dpi", "70");
                    using (var img = Pix.LoadFromFile(sourceFilePath))
                    {
                        using (var page = engine.Process(img))
                        {
                            var text = page.GetText();
                            Console.WriteLine("---Image Text---");
                            Console.WriteLine(text);
                            string txtFilePath = @"textFile.txt";
                            if (!File.Exists(txtFilePath))
                            {
                                using (StreamWriter sw = File.CreateText(txtFilePath))
                                {
                                    sw.WriteLine(text);
                                }
                            }
                            else
                            {
                                using (StreamWriter sw = File.AppendText(txtFilePath))
                                {
                                    sw.Write(text);
                                }
                                File.Delete(di + $"\\screenShootImg.png");
                            }
                            List<List<string>> groups = new List<List<string>>();
                            List<string> current;
                            string word = "Account No:";
                            string trgtLine;
                            string num;
                            string number;
                            foreach (var line in File.ReadAllLines(txtFilePath))
                            {
                                if (line.Contains(word))
                                {
                                    Console.WriteLine("$$$$$$$$$$$$$$$");
                                    trgtLine = line;
                                    current = new List<string>();
                                    groups.Add(current);
                                    ///if(trgtLine.Length > 17)
                                    {
                                        num = trgtLine.Substring(trgtLine.IndexOf(word), 17);
                                        number = Regex.Replace(num, "[^0-9]+", string.Empty);
                                        File.AppendAllText(txtFilePath, $"{number}");

                                        try
                                        {
                                            HttpClient client = new HttpClient();
                                            HttpResponseMessage response = await client.GetAsync($"http://Huddleboardv2:81/api/GetPatientGaps/{number}");
                                            response.EnsureSuccessStatusCode();
                                            string responseBody = await response.Content.ReadAsStringAsync();
                                            File.AppendAllText(txtFilePath, $"[{responseBody}]");

                                            /*client.BaseAddress = new Uri("http://Huddleboardv2:81/api/GetPatientGaps");
                                            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                                            HttpResponseMessage response = client.GetAsync($"/{number}").Result;*/
                                            /*string msg = response.ToString();
                                            if (response.IsSuccessStatusCode)
                                            {
                                                Console.WriteLine(response.Content);
                                                File.AppendAllText(txtFilePath, $"[{msg}]");
                                            }*/
                                        }
                                        catch (Exception ex)
                                        {
                                            ex.ToString();
                                            //File.AppendAllText(txtFilePath, ex.ToString());
                                        }
                                        break;
                                    }
                                }
                                else
                                {
                                    File.WriteAllText(txtFilePath, "Account Number: String.Empty();");
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}