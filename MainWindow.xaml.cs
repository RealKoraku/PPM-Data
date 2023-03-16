using System;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;

namespace ImageLoaderMessage {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {

        public string globalPath = "";

        public MainWindow() {
            InitializeComponent();

        }

        private void muiOpenPPM_Click(object sender, RoutedEventArgs e) {
            //CREATE OPEN FILE DIALOG
            OpenFileDialog openFileDialog = new OpenFileDialog();

            //SETUP PARAMETERS FOR OPEN FILE DIALOG
            openFileDialog.DefaultExt = ".PPM";
            openFileDialog.Filter = "PPM Files (.ppm)|*.ppm";

            //SHOW FILE DIALOG
            bool? result = openFileDialog.ShowDialog();

            //PROCESS DIALOG RESULTS / DETERMINE IF FILE WAS OPENED
            if (result == true) {
                //STORE FILE PATH
                string selectedFile = openFileDialog.FileName;

                globalPath = selectedFile;

                //CALL LOADIMAGE METHOD
                List<byte[]> RGBvalues = new List<byte[]>();

                GetPPMData(selectedFile);
            }
        }

        private void muiSavePPM_Click(object sender, RoutedEventArgs e) {
            if (imgMain.Source == null) {
                return;
            } else {
                SaveFileDialog saveFileDialog = new SaveFileDialog();

                saveFileDialog.DefaultExt = ".PPM";
                saveFileDialog.Filter = "PPM Files (.ppm)|*.ppm";

                bool? result = saveFileDialog.ShowDialog();

                string path = saveFileDialog.FileName;

                if (path != null) {
                    FileStream outfile = new FileStream(@$"{path}", FileMode.Create);

                    string buffer = LoadString(globalPath);

                    char[] bufferChars = buffer.ToCharArray();

                    for (int i = 0; i < bufferChars.Length; i++) {
                        byte data = (byte)bufferChars[i];
                        outfile.WriteByte(data);
                    }

                    outfile.Close();
                }
            }
        }

        private void GetPPMData(string path) {
            bool parser;

            string[] PPMdata = LoadArray(path);

            string fileType = PPMdata[0];

            if ((fileType != "P3" && fileType != "P6") || PPMdata.Length < 5) {
                CharOflow.Foreground = Brushes.Red;
                CharOflow.Content = "Invalid file format";
            } else {

                string comment = PPMdata[1];
                string[] imgRes = PPMdata[2].Split(" ");
                string RGBchannel = PPMdata[3];

                string resHeightStr = imgRes[0];
                string resWidthStr = imgRes[1];
                int resHeight;
                int resWidth;

                parser = int.TryParse(resHeightStr, out resHeight);
                parser = int.TryParse(resWidthStr, out resWidth);

                List<byte[]> RGBvalues = new List<byte[]>();

                if (fileType == "P3") {
                    RGBvalues = ReadP3(PPMdata, resHeight, resWidth);

                } else if (fileType == "P6") {
                    RGBvalues = ReadP6(PPMdata, resHeight, resWidth);
                }

                BitmapMaker PPMbitmap = BuildBitmap(resHeight, resWidth, PPMdata, RGBvalues);
                DisplayBitmap(PPMbitmap);
            }
        }

        private List<byte[]> ReadP3(string[] PPMdata, int resHeight, int resWidth) {
            bool parser;
            List<byte[]> RGBvalues = new List<byte[]>();

            HideOflowLabel();

            for (int line = 4; line < PPMdata.Length - 1; line += 0) {

                byte[] RGB = new byte[3];
                byte RGBbyte = 0;

                for (int rgb = 0; rgb < 3; rgb++) {

                    parser = byte.TryParse(PPMdata[line], out RGBbyte);
                    RGB[rgb] = RGBbyte;
                    line++;

                    if (rgb == 2) {
                        RGBvalues.Add(RGB);
                    }
                }
            }
            return RGBvalues;
        }

        private List<byte[]> ReadP6(string[] PPMdata, int resHeight, int resWidth) {
            bool parser;
            List<byte[]> RGBvalues = new List<byte[]>();

            HideOflowLabel();

            char[] binaryData = PPMdata[4].ToCharArray();

            for (int bytes = 0; bytes < binaryData.Length; bytes += 0) {

                byte[] RGB = new byte[3];
                byte RGBbyte = 0;

                for (int rgb = 0; rgb < 3; rgb++) {

                    RGBbyte = (byte)binaryData[bytes];
                    RGB[rgb] = RGBbyte;
                    bytes++;

                    if (rgb == 2) {
                        RGBvalues.Add(RGB);
                    }
                }
            }
            return RGBvalues;
        }

        private void DisplayBitmap(BitmapMaker PPMbitmap) {
            WriteableBitmap wbmImage = PPMbitmap.MakeBitmap();

            imgMain.Source = wbmImage;
        }

        private string[] LoadArray(string path) {
            string[] lines;
            string data = "";
            string[] records;

            //Read data from file
            FileStream inFile = new FileStream(path, FileMode.Open);

            while (inFile.Position < inFile.Length) {
                data += (char)inFile.ReadByte();
            }//end while

            inFile.Close();

            //Split data into records
            lines = data.Split("\n");

            return lines;
        }

        private string LoadString(string path) {
            string dataString = "";
            
            FileStream inFile = new FileStream(path, FileMode.Open);

            while (inFile.Position < inFile.Length) {
                dataString += (char)inFile.ReadByte();
            }//end while

            inFile.Close();

            return dataString;
        }

        private BitmapMaker BuildBitmap(int resHeight, int resWidth, string[] PPMdata, List<byte[]> RGBvalues) {

            BitmapMaker PPMbitmap = new BitmapMaker(resWidth, resHeight);

            int RGBvalIndex = 0;

            for (int y = 0; y < resHeight; y++) {
                for (int x = 0; x < resWidth; x++) {

                    byte RGBr = RGBvalues[RGBvalIndex][0];
                    byte RGBg = RGBvalues[RGBvalIndex][1];
                    byte RGBb = RGBvalues[RGBvalIndex][2];
                    PPMbitmap.SetPixel(x, y, RGBr, RGBg, RGBb);

                    RGBvalIndex++;
                }
            }

            return PPMbitmap;
        }

        private void TxtBoxMessage_TextChanged(object sender, TextChangedEventArgs e) {
            string message;

            if (TxtBoxMessage.Text.Length > 255) {
                TxtBoxMessage.Foreground = Brushes.Red;
            } else {
                TxtBoxMessage.Foreground = Brushes.Black;
            }

            message = TxtBoxMessage.Text;
        }

        private void Button_Click(object sender, RoutedEventArgs e) {
            if (TxtBoxMessage.Text.Length > 255) {
                CharOflow.Content = "Too many chars!";
                CharOflow.Foreground = Brushes.Red;
                return;
            } else {
                HideOflowLabel();
            }
        }

        private void HideOflowLabel() {
            var converter = new System.Windows.Media.BrushConverter();
            var bgColor = (Brush)converter.ConvertFromString("#FFFFD8A8");

            CharOflow.Foreground = bgColor;
        }
    }
}