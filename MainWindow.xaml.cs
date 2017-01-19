using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using Puma.Net;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.Util;
using System.Drawing;
using System.IO;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Drawing;

namespace OpticalCharacterRecognition
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Bitmap sourceImage;
        Bitmap bitmap, bitmapTemp, cloneBitmap;
        String outputString;
        int count = 0, countPart = 0;
        int countTemp = 0, countDino = 0;
        float index = 0;
        List<float> Max,maxWidth,maxHeight;
        PumaPage inputFile;
        String DinoFileName;

        string PathImagesDinoScan = @"C:\Users\takatanonly\Pictures\ScanSnap\";
        string PathImagesDinoTemplate = @"C:\DinoTemplate\";
        string PathImagesDinoParts = @"C:\DinoParts\";
        //string PathImagestest = @"C:\Users\takatanonly\Documents\Visual Studio 2010\Projects\Project_ChangeBitmapRealtime_Test1\Project_ChangeBitmapRealtime_Test1\Images\";

        Bitmap trex_leg1_temp, trex_leg2_temp, trex_body_temp, trex_arm1_temp, trex_arm2_temp, trex_head_temp;
        Bitmap trex_leg1_flip_temp, trex_leg2_flip_temp, trex_body_flip_temp, trex_arm1_flip_temp, trex_arm2_flip_temp, trex_head_flip_temp;


        public MainWindow()
        {
            InitializeComponent();

            System.Windows.Threading.DispatcherTimer timer_UpdateFile = new System.Windows.Threading.DispatcherTimer();
            timer_UpdateFile.Tick += new EventHandler(Timer_Tick_UpdateFile);
            timer_UpdateFile.Interval = new TimeSpan(0, 0, 0, 1, 0);
            timer_UpdateFile.Start();
        }

        private void Timer_Tick_UpdateFile(object sender, EventArgs e)
        {
            int fileCount = Directory.GetFiles(PathImagesDinoScan).Length;
            if (fileCount > countTemp)
            {
                string[] files = Directory.GetFiles(PathImagesDinoScan);
                List<FileList> objFileList = new List<FileList>();
                foreach (string file in files)
                {
                    DateTime creationTime = System.IO.File.GetCreationTime(file);
                    objFileList.Add(new FileList(file, creationTime));
                    //Console.WriteLine(System.IO.Path.GetFileName(file));
                }

                Console.WriteLine("Sort the list by date descending:");
                objFileList.Sort((x, y) => y.FileDate.CompareTo(x.FileDate));

                try
                {
                    sourceImage = new Bitmap(objFileList[0].FileName);//sourceFilePath);
                    string[] str = objFileList[0].FileName.Split('\\');
                    str = str[str.Length - 1].Split('.');
                    DinoFileName = str[0];
                    Bitmap current = (Bitmap)sourceImage.Clone();

                    Image<Bgr, Byte> imgInput = new Image<Bgr, byte>(current);
                    bitmap = imgInput.ToBitmap();
                    Image<Bgr, Byte> img = new Image<Bgr, byte>(current);
                    Image<Gray, Byte> gray = img.Convert<Gray, Byte>().PyrDown().PyrUp();
                    Image<Gray, Byte> invert = gray.Not();
                    bitmapTemp = invert.ToBitmap();

                    ///Find Rectangle Function
                    Image<Gray, Byte> cannyEdges = invert.Canny(new Gray(180), new Gray(120));
                    CvInvoke.cvShowImage("Edge image", cannyEdges);
                    List<MCvBox2D> rectangleList = new List<MCvBox2D>();
                    MemStorage storage = new MemStorage();

                    for (Contour<System.Drawing.Point> contours = cannyEdges.FindContours(); contours != null; contours = contours.HNext)
                    {
                        Contour<System.Drawing.Point> currentContour = contours.ApproxPoly(contours.Perimeter * 0.05, storage);
                        if (contours.Area > 8000) //ตรวจสอบขนาดของ contour ต้องมากกว่า 50
                        {
                            if (currentContour.Total == 4) //ตรวจสอบจำนวนจุดของ contour ==  4
                            {
                                bool isRectangle = true;
                                System.Drawing.Point[] pts = currentContour.ToArray();
                                LineSegment2D[] edges = Emgu.CV.PointCollection.PolyLine(pts, true);

                                for (int i = 0; i < edges.Length; i++)
                                {
                                    double angle = Math.Abs(edges[(i + 1) % edges.Length].GetExteriorAngleDegree(edges[i]));
                                    if (angle < 80 || angle > 100) // ตรวจสอบ องศา
                                    {
                                        isRectangle = false;
                                        break;
                                    }
                                }
                                if (isRectangle) //isRectangle == true
                                {
                                    rectangleList.Add(currentContour.GetMinAreaRect());
                                }
                            }
                        }
                    }

                    List<System.Drawing.Rectangle> cloneRect = new List<System.Drawing.Rectangle>();
                    Max = new List<float>();
                    maxWidth = new List<float>();
                    maxHeight = new List<float>();
                    float boxWidth, boxHeight, boxX, boxY;
                    outputString = "";

                    foreach (MCvBox2D box in rectangleList) //วาดกรอบสี่เหลี่ยมรอบๆ วัตถุที่เป็นสี่เหลี่ยม
                    {
                        maxWidth.Add(box.size.Width);
                        maxHeight.Add(box.size.Height);
                    }
                    maxWidth.Sort();
                    maxHeight.Sort();
                    foreach (MCvBox2D box in rectangleList) //วาดกรอบสี่เหลี่ยมรอบๆ วัตถุที่เป็นสี่เหลี่ยม
                    {
                        if (count % 2 == 0)
                        {
                            boxX = box.center.X;
                            boxY = box.center.Y;

                            if (maxWidth[maxWidth.Count - 1] > maxHeight[maxHeight.Count - 1])
                            {
                                //boxX = box.center.X;
                                //boxY = box.center.Y;
                                boxWidth = box.size.Width;
                                boxHeight = box.size.Height;
                            }
                            else//switch
                            {
                                //boxX = box.center.Y;
                                //boxY = box.center.X;
                                boxWidth = box.size.Height;
                                boxHeight = box.size.Width;
                            }
                            Max.Add(boxWidth);

                            cloneRect.Add(new System.Drawing.Rectangle((int)(boxX - (boxWidth / 2)), (int)(boxY - (boxHeight / 2)), (int)(boxWidth), (int)(boxHeight)));//(int)(box.size.Width), (int)(box.size.Height));

                            System.Drawing.Imaging.PixelFormat format = bitmapTemp.PixelFormat;
                            try
                            {
                                cloneBitmap = bitmapTemp.Clone(cloneRect[cloneRect.Count - 1], format);
                                
                                if (cloneBitmap.Size.Height > 50 && cloneBitmap.Size.Height < 200)
                                {
                                    inputFile = new PumaPage(cloneBitmap);
                                    inputFile.FileFormat = PumaFileFormat.TxtAscii;
                                    inputFile.Language = PumaLanguage.English;
                                    outputString += inputFile.RecognizeToString();
                                    inputFile.Dispose();
                                }
                            }
                            catch
                            {
                                Console.WriteLine("Catch");
                                //MessageBox.Show("Error Picture Pixcel is under " + Width + " x " + Height);
                            }
                        }
                        count++;
                    }
                    //Height = Height , Width = Width
                    Max.Sort();
                    foreach (System.Drawing.Rectangle box in cloneRect) //วาดกรอบสี่เหลี่ยมรอบๆ วัตถุที่เป็นสี่เหลี่ยม
                    {
                        if (box.Width == (int)(Max[Max.Count - 2]) && box.Width > 1000 || box.Width == (int)(Max[Max.Count - 1]) && box.Width > 1000)//Area of Paint
                        {
                            try
                            {
                                System.Drawing.Imaging.PixelFormat format = bitmap.PixelFormat;
                                System.Drawing.Rectangle boxUSE = box;
                                cloneBitmap = bitmap.Clone(boxUSE, format);

                                if (outputString.Contains("I") && outputString.Contains("E"))
                                {
                                    //MessageBox.Show(outputString + " Trex");

                                    cloneBitmap.RotateFlip(System.Drawing.RotateFlipType.Rotate90FlipNone);
                                    Bitmap resized = new Bitmap(cloneBitmap, new System.Drawing.Size(2480, 3508));

                                    //resized.Save("C:/Users/takatanonly/Desktop/bitmapUSE.png");

                                    ///Dino Flip
                                    ////////////////////body
                                    Bitmap bmpScan = resized;

                                    Bitmap bmpTemplate = new Bitmap(PathImagesDinoTemplate + "trex.png");
                                    bmpTemplate = new Bitmap(bmpTemplate, new System.Drawing.Size(2480, 3508));

                                    Bitmap bmpPart = new Bitmap(PathImagesDinoParts + "trex_body_flip.png");
                                    ChangeColor(bmpTemplate, bmpPart, bmpScan, "body_flip");

                                    ////////////////////////////////////Head

                                    //resize image

                                    //Bitmap temp = (Bitmap)System.Drawing.Image.FromFile("d:\\source4.png");

                                    bmpScan = new Bitmap(bmpScan, new System.Drawing.Size(bmpScan.Width / 8, bmpScan.Height / 8));

                                    Bitmap temp2 = new Bitmap(PathImagesDinoTemplate + "trex.png");

                                    bmpTemplate = new Bitmap(temp2, new System.Drawing.Size(temp2.Width / 8, temp2.Height / 8));

                                    Bitmap temp3 = new Bitmap(PathImagesDinoParts + "trex_head_flip.png");
                                    bmpPart = new Bitmap(temp3, new System.Drawing.Size(temp3.Width / 8, temp3.Height / 8));

                                    //bmptemp = (Bitmap)System.Drawing.Image.FromFile("d:\\trex_head_flip.png");
                                    ChangeColor(bmpTemplate, bmpPart, bmpScan, "head_flip");

                                    ////////////////////////////////////trex_arm1_flip
                                    temp3 = new Bitmap(PathImagesDinoParts + "trex_arm1_flip.png");
                                    bmpPart = new Bitmap(temp3, new System.Drawing.Size(temp3.Width / 8, temp3.Height / 8));
                                    ChangeColor(bmpTemplate, bmpPart, bmpScan, "arm1_flip");
                                    ////////////////////////////////////trex_arm2_flip
                                    temp3 = new Bitmap(PathImagesDinoParts + "trex_arm2_flip.png");
                                    bmpPart = new Bitmap(temp3, new System.Drawing.Size(temp3.Width / 8, temp3.Height / 8));
                                    ChangeColor(bmpTemplate, bmpPart, bmpScan, "arm2_flip");
                                    ////////////////////////////////////trex_arm1_flip
                                    temp3 = new Bitmap(PathImagesDinoParts + "trex_leg1_flip.png");
                                    bmpPart = new Bitmap(temp3, new System.Drawing.Size(temp3.Width / 8, temp3.Height / 8));
                                    ChangeColor(bmpTemplate, bmpPart, bmpScan, "leg1_flip");
                                    ////////////////////////////////////trex_arm1_flip
                                    temp3 = new Bitmap(PathImagesDinoParts + "trex_leg2_flip.png");
                                    bmpPart = new Bitmap(temp3, new System.Drawing.Size(temp3.Width / 8, temp3.Height / 8));
                                    ChangeColor(bmpTemplate, bmpPart, bmpScan, "leg2_flip");


                                    //Dino Non Flip
                                    ////////////////////body
                                    resized.RotateFlip(System.Drawing.RotateFlipType.Rotate180FlipY);
                                    bmpScan = resized;

                                    bmpTemplate = new Bitmap(PathImagesDinoTemplate + "trex.png");
                                    bmpTemplate = new Bitmap(bmpTemplate, new System.Drawing.Size(2480, 3508));
                                    bmpTemplate.RotateFlip(System.Drawing.RotateFlipType.Rotate180FlipY);

                                    bmpPart = new Bitmap(PathImagesDinoParts + "trex_body.png");
                                    ChangeColor(bmpTemplate, bmpPart, bmpScan, "body");

                                    //resize image

                                    //Bitmap temp = (Bitmap)System.Drawing.Image.FromFile("d:\\source4.png");

                                    bmpScan = new Bitmap(bmpScan, new System.Drawing.Size(bmpScan.Width / 8, bmpScan.Height / 8));

                                    temp2 = new Bitmap(PathImagesDinoTemplate + "trex.png");

                                    bmpTemplate = new Bitmap(temp2, new System.Drawing.Size(temp2.Width / 8, temp2.Height / 8));

                                    temp3 = new Bitmap(PathImagesDinoParts + "trex_head.png");
                                    bmpPart = new Bitmap(temp3, new System.Drawing.Size(temp3.Width / 8, temp3.Height / 8));

                                    //bmptemp = (Bitmap)System.Drawing.Image.FromFile("d:\\trex_head_flip.png");
                                    ChangeColor(bmpTemplate, bmpPart, bmpScan, "head");

                                    ////////////////////////////////////trex_arm1_flip
                                    temp3 = new Bitmap(PathImagesDinoParts + "trex_arm1.png");
                                    bmpPart = new Bitmap(temp3, new System.Drawing.Size(temp3.Width / 8, temp3.Height / 8));
                                    ChangeColor(bmpTemplate, bmpPart, bmpScan, "arm1");
                                    ////////////////////////////////////trex_arm2_flip
                                    temp3 = new Bitmap(PathImagesDinoParts + "trex_arm2.png");
                                    bmpPart = new Bitmap(temp3, new System.Drawing.Size(temp3.Width / 8, temp3.Height / 8));
                                    ChangeColor(bmpTemplate, bmpPart, bmpScan, "arm2");
                                    ////////////////////////////////////trex_arm1_flip
                                    temp3 = new Bitmap(PathImagesDinoParts + "trex_leg1.png");
                                    bmpPart = new Bitmap(temp3, new System.Drawing.Size(temp3.Width / 8, temp3.Height / 8));
                                    ChangeColor(bmpTemplate, bmpPart, bmpScan, "leg1");
                                    ////////////////////////////////////trex_arm1_flip
                                    temp3 = new Bitmap(PathImagesDinoParts + "trex_leg2.png");
                                    bmpPart = new Bitmap(temp3, new System.Drawing.Size(temp3.Width / 8, temp3.Height / 8));
                                    ChangeColor(bmpTemplate, bmpPart, bmpScan, "leg2");

                                    bmpScan.Save("C:/DinoShow/" + DinoFileName + ".png");
                                    countDino++;
                                    if (countDino == 10) countDino = 0;
                                    Console.WriteLine("countDino = " + countDino.ToString(), "countDino");
                                    
                                }
                                else
                                {
                                    MessageBox.Show("Please Try Again");

                                }
                                countTemp = fileCount;
                                break;
                            }
                            catch(Exception ex)
                            {
                                MessageBox.Show("Catch " + ex.ToString());
                                //Console.WriteLine("Catch " + ex.ToString());
                          }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Catch " + ex.ToString());
                    //Console.WriteLine("Exception = " + ex.ToString());
                }
            }
            Console.WriteLine("FileCount = " + fileCount, "FileCount");
        }

        public class FileList
        {
            public string FileName { get; set; }
            public DateTime FileDate { get; set; }

            public FileList(string filename, DateTime filedate)
            {
                FileName = filename;
                FileDate = filedate;
            }
        }
    

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Multiselect = false;
            Nullable<bool> isSelected = fileDialog.ShowDialog();
            if (isSelected == true)
            {
                String filePath = fileDialog.FileName;
                textBox1.Text = filePath;
            }
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            string sourceFilePath = textBox1.Text.Trim();
            sourceImage = new Bitmap(sourceFilePath);
            Bitmap current = (Bitmap)sourceImage.Clone();
           
            Image<Bgr, Byte> imgInput = new Image<Bgr, byte>(current);
            bitmap = imgInput.ToBitmap();
            Image<Bgr, Byte> img = new Image<Bgr, byte>(current);
            Image<Gray, Byte> gray = img.Convert<Gray, Byte>().PyrDown().PyrUp();
            Image<Gray, Byte> invert = gray.Not();
            bitmapTemp = invert.ToBitmap();

            ///Find Rectangle Function
            Image<Gray, Byte> cannyEdges = invert.Canny(new Gray(180), new Gray(120));
            CvInvoke.cvShowImage("Edge image", cannyEdges);
            
            List<MCvBox2D> rectangleList = new List<MCvBox2D>();
            MemStorage storage = new MemStorage();

            for (Contour<System.Drawing.Point> contours = cannyEdges.FindContours(); contours != null; contours = contours.HNext)
            {

                Contour<System.Drawing.Point> currentContour = contours.ApproxPoly(contours.Perimeter * 0.05, storage);
                if (contours.Area > 8000) //ตรวจสอบขนาดของ contour ต้องมากกว่า 50
                {
                    if (currentContour.Total == 4) //ตรวจสอบจำนวนจุดของ contour ==  4
                    {
                        bool isRectangle = true;
                        System.Drawing.Point[] pts = currentContour.ToArray();
                        LineSegment2D[] edges = Emgu.CV.PointCollection.PolyLine(pts, true);

                        for (int i = 0; i < edges.Length; i++)
                        {
                            double angle = Math.Abs(edges[(i + 1) % edges.Length].GetExteriorAngleDegree(edges[i]));
                            if (angle < 80 || angle > 100) // ตรวจสอบ องศา
                            {
                                isRectangle = false;
                                break;
                            }
                        }

                        if (isRectangle) //isRectangle == true
                        {
                            rectangleList.Add(currentContour.GetMinAreaRect());
                        }
                    }
                }
            }

            //int countinvert
            //float index 0;
            //List<float> Max = new List<float>();

            List<System.Drawing.Rectangle> cloneRect = new List<System.Drawing.Rectangle>();
            Max = new List<float>();
            maxWidth = new List<float>();
            maxHeight = new List<float>();
            float boxWidth, boxHeight, boxX, boxY;
            outputString = "";
            foreach (MCvBox2D box in rectangleList) //วาดกรอบสี่เหลี่ยมรอบๆ วัตถุที่เป็นสี่เหลี่ยม
            {
                maxWidth.Add(box.size.Width);
                maxHeight.Add(box.size.Height);
            }
            maxWidth.Sort();
            maxHeight.Sort();
            foreach (MCvBox2D box in rectangleList) //วาดกรอบสี่เหลี่ยมรอบๆ วัตถุที่เป็นสี่เหลี่ยม
            {
                if (count % 2 == 0)
                {
                    boxX = box.center.X;
                    boxY = box.center.Y;

                    if (maxWidth[maxWidth.Count - 1] > maxHeight[maxHeight.Count - 1])
                    {
                        //boxX = box.center.X;
                        //boxY = box.center.Y;
                        boxWidth = box.size.Width;
                        boxHeight = box.size.Height;
                    }
                    else//switch
                    {
                        //boxX = box.center.Y;
                        //boxY = box.center.X;
                        boxWidth = box.size.Height;
                        boxHeight = box.size.Width;
                    }
                    
                    Max.Add(boxWidth);

                    cloneRect.Add(new System.Drawing.Rectangle((int)(boxX - (boxWidth / 2)), (int)(boxY - (boxHeight / 2)), (int)(boxWidth), (int)(boxHeight)));//(int)(box.size.Width), (int)(box.size.Height));

                    System.Drawing.Imaging.PixelFormat format = bitmapTemp.PixelFormat;
                    try
                    {
                        cloneBitmap = bitmapTemp.Clone(cloneRect[cloneRect.Count - 1], format);
                        if (cloneBitmap.Size.Height > 50 && cloneBitmap.Size.Height < 200)
                        {
                            inputFile = new PumaPage(cloneBitmap);
                            inputFile.FileFormat = PumaFileFormat.TxtAscii;
                            inputFile.Language = PumaLanguage.English;
                            outputString += inputFile.RecognizeToString();
                            inputFile.Dispose();
                        }
                    }
                    catch
                    {
                        Console.WriteLine("Catch");
                        //MessageBox.Show("Error Picture Pixcel is under " + Width + " x " + Height);
                    }
                }
                count++;
            }
            //Height = Height , Width = Width
            Max.Sort();
            foreach (System.Drawing.Rectangle box in cloneRect) //วาดกรอบสี่เหลี่ยมรอบๆ วัตถุที่เป็นสี่เหลี่ยม
            {
                if (box.Width == (int)(Max[Max.Count - 2]) && box.Width > 1000 || box.Width == (int)(Max[Max.Count - 1]) && box.Width > 1000)//Area of Paint
                {
                    System.Drawing.Imaging.PixelFormat format = bitmap.PixelFormat;
                    //double gapSize = 25;
                    //int ImageWidth = 2480;
                    //int ImageHeight = 3508;
                    System.Drawing.Rectangle boxUSE = box;
                    
                    cloneBitmap = bitmap.Clone(boxUSE, format);
                    //cloneBitmap = bitmap.Clone(new System.Drawing.Rectangle(197, 197, ImageWidth, ImageHeight), format);

                    cloneBitmap.RotateFlip(System.Drawing.RotateFlipType.Rotate90FlipNone);
                   
                    Bitmap resized = new Bitmap(cloneBitmap, new System.Drawing.Size(2480, 3508));
                    resized.Save("C:/Users/takatanonly/Desktop/bitmapUSE.png");

                    break;
                }
            }
            //MessageBox.Show(Max[Max.Count - 2].ToString());
            //MessageBox.Show(outputString);

            if (outputString.Contains("I") && outputString.Contains("E"))
            {
                MessageBox.Show(outputString + " Trex");
                //ChangeColor(cloneBitmap);
            }
            else MessageBox.Show("Please Try Again");
        }

        BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();

                return bitmapimage;
            }
        }

        public void CopyPixel(Bitmap bmp, System.Drawing.Rectangle box)
        {
            //Bitmap bmp = (Bitmap)Image.FromFile("D:/DaTaS/งานEdgeDetect/workEdge.jpg");
            LockBitmap lockBitmap = new LockBitmap(bmp);
            lockBitmap.LockBits();

            System.Drawing.Color color = System.Drawing.Color.FromArgb(255, 255, 255, 255);
            
            lockBitmap.UnlockBits();
           
            bmp.Save("D:/DaTaS/งานEdgeDetect/result.jpg");
        }

        public void ChangeColor(Bitmap bmptemptemp, Bitmap bmptemp, Bitmap img, String Name)
        {
            LockBitmap lockbmptemptemp = new LockBitmap(bmptemptemp);
            LockBitmap lockbmptemp = new LockBitmap(bmptemp);
            LockBitmap lockbmpimg = new LockBitmap(img);

            lockbmptemptemp.LockBits();
            lockbmptemp.LockBits();
            lockbmpimg.LockBits();

            System.Drawing.Color compareWhite = System.Drawing.Color.FromArgb(255, 255, 255, 255);
            System.Drawing.Color compareBlack = System.Drawing.Color.FromArgb(255, 0, 0, 0);

            for (int y = 0; y < lockbmptemp.Height; y++)
            {
                for (int x = 0; x < lockbmptemp.Width; x++)
                {
                    double diffColor = ColourDistance(lockbmptemptemp.GetPixel(x, y), lockbmpimg.GetPixel(x, y));
                    if (lockbmptemptemp.GetPixel(x, y) == compareBlack && Name.Contains("body"))//countPart == 0)// && diffColor < 400)
                    {
                        //lockbmptemp.SetPixel(x, y, System.Drawing.Color.Red);
                        continue;
                    }
                    else if (lockbmptemp.GetPixel(x, y) == compareWhite)// && lockbmpimg.GetPixel(x, y) != compareBlack)
                    {   //lockbmptemp.SetPixel(x, y, System.Drawing.Color.Red);
                        lockbmptemp.SetPixel(x, y, lockbmpimg.GetPixel(x, y));
                    }
                }
            }
            lockbmptemptemp.UnlockBits();
            lockbmptemp.UnlockBits();
            lockbmpimg.UnlockBits();
           
            Bitmap resultResized = new Bitmap(bmptemp, new System.Drawing.Size(2480, 3508));
           
            resultResized.Save("C:\\DinoUse\\" + DinoFileName + Name + ".png");
        }

        public double ColourDistance(System.Drawing.Color e1, System.Drawing.Color e2)
        {
            long rmean = ((long)e1.R + (long)e2.R) / 2;
            long r = (long)e1.R - (long)e2.R;
            long g = (long)e1.G - (long)e2.G;
            long b = (long)e1.B - (long)e2.B;
            return Math.Sqrt((((512 + rmean) * r * r) >> 8) + 4 * g * g + (((767 - rmean) * b * b) >> 8));
        }
    }

    public class LockBitmap
    {
        Bitmap source = null;
        IntPtr Iptr = IntPtr.Zero;
        BitmapData bitmapData = null;

        public byte[] Pixels { get; set; }
        public int Depth { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }

        public LockBitmap(Bitmap source)
        {
            this.source = source;
        }

        /// <summary>
        /// Lock bitmap data
        /// </summary>
        public void LockBits()
        {
            try
            {
                // Get width and height of bitmap
                Width = source.Width;
                Height = source.Height;

                // get total locked pixels count
                int PixelCount = Width * Height;

                // Create rectangle to lock
                System.Drawing.Rectangle rect = new System.Drawing.Rectangle(0, 0, Width, Height);

                // get source bitmap pixel format size
                Depth = System.Drawing.Bitmap.GetPixelFormatSize(source.PixelFormat);

                // Check if bpp (Bits Per Pixel) is 8, 24, or 32
                if (Depth != 8 && Depth != 24 && Depth != 32)
                {
                    throw new ArgumentException("Only 8, 24 and 32 bpp images are supported.");
                }

                // Lock bitmap and return bitmap data
                bitmapData = source.LockBits(rect, ImageLockMode.ReadWrite,
                                             source.PixelFormat);

                // create byte array to copy pixel values
                int step = Depth / 8;
                Pixels = new byte[PixelCount * step];
                Iptr = bitmapData.Scan0;

                // Copy data from pointer to array
                Marshal.Copy(Iptr, Pixels, 0, Pixels.Length);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Unlock bitmap data
        /// </summary>
        public void UnlockBits()
        {
            try
            {
                // Copy data from byte array to pointer
                Marshal.Copy(Pixels, 0, Iptr, Pixels.Length);

                // Unlock bitmap data
                source.UnlockBits(bitmapData);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Get the color of the specified pixel
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public System.Drawing.Color GetPixel(int x, int y)
        {
            System.Drawing.Color clr = System.Drawing.Color.Empty;

            // Get color components count
            int cCount = Depth / 8;

            // Get start index of the specified pixel
            int i = ((y * Width) + x) * cCount;

            if (i > Pixels.Length - cCount)
                throw new IndexOutOfRangeException();

            if (Depth == 32) // For 32 bpp get Red, Green, Blue and Alpha
            {
                byte b = Pixels[i];
                byte g = Pixels[i + 1];
                byte r = Pixels[i + 2];
                byte a = Pixels[i + 3]; // a
                clr = System.Drawing.Color.FromArgb(a, r, g, b);
            }
            if (Depth == 24) // For 24 bpp get Red, Green and Blue
            {
                byte b = Pixels[i];
                byte g = Pixels[i + 1];
                byte r = Pixels[i + 2];
                clr = System.Drawing.Color.FromArgb(r, g, b);
            }
            if (Depth == 8)
            // For 8 bpp get color value (Red, Green and Blue values are the same)
            {
                byte c = Pixels[i];
                clr = System.Drawing.Color.FromArgb(c, c, c);
            }
            return clr;
        }

        public byte GetPixelData(int x, int y)
        {
            byte c;// data;

            System.Drawing.Color clr = System.Drawing.Color.Empty;

            // Get color components count
            int cCount = Depth / 8;

            // Get start index of the specified pixel
            int i = ((y * Width) + x) * cCount;

            if (i > Pixels.Length - cCount)
                throw new IndexOutOfRangeException();

            if (Depth == 8)
            // For 8 bpp get color value (Red, Green and Blue values are the same)
            {
                c = Pixels[i];
                return c;
            }
            else return 0;
        }

        /// <summary>
        /// Set the color of the specified pixel
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="color"></param>
        public void SetPixel(int x, int y, System.Drawing.Color color)
        {
            // Get color components count
            int cCount = Depth / 8;

            // Get start index of the specified pixel
            int i = ((y * Width) + x) * cCount;

            if (Depth == 32) // For 32 bpp set Red, Green, Blue and Alpha
            {
                Pixels[i] = color.B;
                Pixels[i + 1] = color.G;
                Pixels[i + 2] = color.R;
                Pixels[i + 3] = color.A;
            }
            if (Depth == 24) // For 24 bpp set Red, Green and Blue
            {
                Pixels[i] = color.B;
                Pixels[i + 1] = color.G;
                Pixels[i + 2] = color.R;
            }
            if (Depth == 8)
            // For 8 bpp set color value (Red, Green and Blue values are the same)
            {
                Pixels[i] = color.B;
            }
        }


    }
}
