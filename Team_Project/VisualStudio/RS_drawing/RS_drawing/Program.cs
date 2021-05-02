using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using ABB.Robotics.Controllers;
using ABB.Robotics.Controllers.Discovery;
using ABB.Robotics.Controllers.RapidDomain;
using System.Drawing.Drawing2D;
using System.IO;


namespace RS_drawing
{
    class Program
    {
        // Default setting - Robot will draw anything that is not white. It can be reversed so white lines only will be drawn
        // To draw white lines only change 0 and 1 at row 114 like this : white only      - array[x, y] = *(scan0Ptr + x + y * stride) > -100 ? 1 : 0;
        //                                                                default setting - array[x, y] = *(scan0Ptr + x + y * stride) > -100 ? 0 : 1;



        int max_path_number = 1900;                                                       // Maximum number of drawn paths - number between 1-1999
        string image_path = @"C:\Users\honza\Downloads\test.png";                         // Path of original image        - must be .png/.jpg/.bmp of any size
        string resized_image_path = @"C:\Users\honza\Downloads\newimage.png";             // Path of resized image         - can be any path, just to save resized image
                                                                                           
        List<string> stringList = new List<string>();
        List<string> pathList = new List<string>();
        List<string> pathcountList = new List<string>();
        List<string> x_Path = new List<string>();
        List<string> y_Path = new List<string>();
        List<int> x_string = new List<int>();
        List<int> y_string = new List<int>();
        int counter = 0;

        int x_size;
        int y_size;
        int resize = 0;

        private Controller controller = null;
        Controller con;
        private NetworkScanner scanner;

        static void Main()
        {
            Program start = new Program();
        }

        public Program()
        {
            this.Image_load();
        }

        public void Image_load()
        {
            var array = GetBits(image_path);
            var h = array.GetLength(0);
            var w = array.GetLength(1);

            if (resize == 0)
            {
                x_size = w;
                y_size = h;
                while (x_size > 310 || y_size > 310)
                {
                    x_size = (int)Math.Ceiling((x_size * 0.95));
                    y_size = (int)Math.Ceiling((y_size * 0.95));
                }
            }

            if (resize == 1 || h > 310 || w > 310)
            {
                Image image = Image.FromStream(File.OpenRead(image_path));
                Bitmap b = new Bitmap(image);
                Image i = ResizeImage(b, new Size(x_size, y_size));
                i.Save(resized_image_path);
                array = GetBits(resized_image_path);
                h = array.GetLength(0);
                w = array.GetLength(1);
                resize = 0;
            }

            for (int i = 1; i < w-1; i++)
            {
                for (int j = 1; j < h-1; j++)
                {
                    if (array[j, i] == 1)
                    {
                        stringList.Add(i.ToString());
                        stringList.Add(j.ToString());
                        counter++;
                    }
                }
            }
            this.Path();
        }

        public unsafe static int[,] GetBits(string path)
        {
            using (var orig = new Bitmap(path))
            {
                orig.RotateFlip(RotateFlipType.Rotate180FlipY);
                var bounds = new Rectangle(0, 0, orig.Width, orig.Height);
                var bitmapData = orig.LockBits(bounds, ImageLockMode.ReadWrite, PixelFormat.Format32bppPArgb);

                try
                {
                    var scan0Ptr = (int*)bitmapData.Scan0;
                    var stride = bitmapData.Stride / 4;
                    var black = Color.Black.ToArgb();
                    var array = new int[orig.Width, orig.Height];

                    for (var y = 0; y < bounds.Bottom; y++)
                        for (var x = 0; x < bounds.Right; x++)
                            array[x, y] = *(scan0Ptr + x + y * stride) > -10000 ? 0 : 1;

                    return array;
                }
                finally
                {
                    orig.UnlockBits(bitmapData);
                }
            }
        }

        private static Image ResizeImage(Image imgToResize, Size size)
        {
            int sourceWidth = imgToResize.Width;
            int sourceHeight = imgToResize.Height;
            float nPercent = 0;
            float nPercentW = 0;
            float nPercentH = 0;
            nPercentW = ((float)size.Width / (float)sourceWidth);
            nPercentH = ((float)size.Height / (float)sourceHeight);
            if (nPercentH < nPercentW)
                nPercent = nPercentH;
            else
                nPercent = nPercentW;
            int destWidth = (int)(sourceWidth * nPercent);
            int destHeight = (int)(sourceHeight * nPercent);
            Bitmap b = new Bitmap(destWidth, destHeight);
            Graphics g = Graphics.FromImage((System.Drawing.Image)b);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(imgToResize, 0, 0, destWidth, destHeight);
            g.Dispose();
            return (Image)b;
        }

        public void Path()
        {
            for (int i = 0; i < counter; i++)
            {
                x_string.Add(int.Parse(stringList[2 * i]));
                y_string.Add(int.Parse(stringList[2 * i + 1]));
            }

            x_string.Add(0);
            y_string.Add(0);

            for (int i = 0; i < counter; i++)
            {
                if (i == 0 && ((y_string[i] + 1) != y_string[i + 1]))
                {
                    pathList.Add("3");
                }

                else
                {
                    if (x_string[i] == x_string[i + 1])  // podm pro x = x+1 + 1
                    {
                        if ((y_string[i] + 1) == y_string[i + 1]) // podm pro y = y+1 + 1   
                        {
                            pathList.Add("1");
                        }

                        else
                        {
                            if (i > 0)
                            {
                                if (y_string[i - 1] == (y_string[i] - 1))
                                {
                                    pathList.Add("2");
                                }

                                else
                                {
                                    pathList.Add("3");
                                }
                            }

                            else
                            {
                                pathList.Add("3");
                            }
                        }
                    }

                    else
                    {
                        if (y_string[i - 1] == (y_string[i] - 1))
                        {
                            pathList.Add("2");
                        }

                        else
                        {
                            pathList.Add("3");
                        }
                    }
                }
            }

            int new_path = 0;

            for (int j = 0; j < x_string.Count - 1; j++)
            {

                if (int.Parse(pathList[j]) == 3)
                {
                    x_Path.Add((x_string[j]*0.5).ToString());
                    y_Path.Add((y_string[j]*0.5).ToString());
                    pathcountList.Add("3");
                    new_path = 0;
                }

                if (int.Parse(pathList[j]) == 1)
                {
                    if (new_path == 0)
                    {
                        x_Path.Add((x_string[j] * 0.5).ToString());
                        y_Path.Add((y_string[j] * 0.5).ToString());
                        pathcountList.Add("1");
                        new_path = 1;
                    }
                }

                if (int.Parse(pathList[j]) == 2)
                {
                    x_Path.Add((x_string[j] * 0.5).ToString());
                    y_Path.Add((y_string[j] * 0.5).ToString());
                    pathcountList.Add("2");
                    new_path = 0;
                }
            }
            this.Size_check();
        }

        public void Size_check()
        {
            if (pathcountList.Count > max_path_number)
            {
                x_size = (int)Math.Ceiling((x_size * 0.95));
                y_size = (int)Math.Ceiling((y_size * 0.95));

                x_Path.Clear();
                y_Path.Clear();
                pathList.Clear();
                pathcountList.Clear();
                stringList.Clear();
                x_string.Clear();
                y_string.Clear();
                counter = 0;

                resize = 1;
                this.Image_load();
            }
            this.Robot();
        }

        public void Robot()
        {
                 scanner = new NetworkScanner();
                 scanner.Scan();
                 ControllerInfoCollection Controllers = scanner.Controllers;

                 foreach (ControllerInfo info in Controllers)
                 {
                     con = ControllerFactory.CreateFrom(info);

                     if (info.Availability == Availability.Available)
                     {
                         con.Logon(UserInfo.DefaultUser);
                         this.Main_start_app();
                     }
                 } 
        }

        public void Main_start_app()
        {
            try
            {
                if (con.OperatingMode == ControllerOperatingMode.Auto)
                {
                    this.Send_abb_thread();
                }
            }
            catch (System.Exception)
            {
            }
        }

        public void Send_abb_thread()
        {
            if (con.OperatingMode == ControllerOperatingMode.Auto)
            {
                RapidData x_nm = con.Rapid.GetRapidData("T_ROB1", "Module1", "x_num");
                RapidData y_nm = con.Rapid.GetRapidData("T_ROB1", "Module1", "y_num");
                RapidData coun = con.Rapid.GetRapidData("T_ROB1", "Module1", "count");
                RapidData num_start = con.Rapid.GetRapidData("T_ROB1", "Module1", "START");
                RapidData path_type = con.Rapid.GetRapidData("T_ROB1", "Module1", "states");

                Num count_send = Num.Parse(pathcountList.Count.ToString());
                Num start = Num.Parse("1");

                using (Mastership master = Mastership.Request(con.Rapid))
                {
                    for (int i = 0; i < pathcountList.Count;)
                    {
                        try
                        {
                            Num x_send = Num.Parse(x_Path[i]);
                            x_nm.WriteItem(x_send, i);

                            Num y_send = Num.Parse(y_Path[i]);
                            y_nm.WriteItem(y_send, i);

                            Num path_type_send = Num.Parse(pathcountList[i]);

                            path_type.WriteItem(path_type_send, i);
                            i++;
                        }

                        catch
                        {
                        }
                    }
                    coun.Value = count_send;
                    num_start.Value = start;
                }
            }
            this.Abb_logoff();
        }

        public void Abb_logoff()
        {
           this.con.Logoff();
        }
    }
}
