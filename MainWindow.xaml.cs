using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ImageLoaderMessage {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {

        public string globalPath = "";
        public BitmapMaker PPMbitmap = new BitmapMaker(0, 0);
        public BitmapMaker publicEncryptedBitmap;
        public string[] publicEncryptedPPM;   

        public MainWindow() {
            InitializeComponent();

        }

        #region File dialogs / Open / Save

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

                string[] PPMdata = GetPPMData(selectedFile);
                BuildPPM(PPMdata);
            }
        }

        private void muiSaveP3_Click(object sender, RoutedEventArgs e) {
            if (imgMain.Source == null) {
                return;
            } else if (publicEncryptedBitmap == null) {
                //buffer = LoadString(globalPath);
                CharOflow.Content = "Encrypt file first";
                CharOflow.Foreground = Brushes.Red;
                return;

            } else {

                publicEncryptedPPM = BitmapToP3(publicEncryptedBitmap);

                HideOflowLabel();

                SaveFileDialog saveFileDialog = new SaveFileDialog();

                saveFileDialog.DefaultExt = ".PPM";
                saveFileDialog.Filter = "PPM Files (.ppm)|*.ppm";

                bool? result = saveFileDialog.ShowDialog();

                string path = saveFileDialog.FileName;

                if (path != null) {
                    FileStream outfile = new FileStream(@$"{path}", FileMode.Create);

                    string buffer = "";

                    for (int line = 0; line < publicEncryptedPPM.Length; line++) {
                        buffer += publicEncryptedPPM[line];
                    }

                    char[] bufferChars = buffer.ToCharArray();

                    for (int i = 0; i < bufferChars.Length; i++) {
                        byte data = (byte)bufferChars[i];
                        outfile.WriteByte(data);
                    }
                    outfile.Close();
                }
            }
        }

        private void muiSaveP6_Click(object sender, RoutedEventArgs e) {
            if (imgMain.Source == null) {
                return;
            } else if (publicEncryptedPPM == null) {
                //buffer = LoadString(globalPath);
                CharOflow.Content = "Encrypt file first";
                CharOflow.Foreground = Brushes.Red;
                return;

            } else {

                publicEncryptedPPM = BitmapToP6(publicEncryptedBitmap);

                HideOflowLabel();

                //SaveFileDialog saveFileDialog = new SaveFileDialog();
                //
                //saveFileDialog.DefaultExt = ".PPM";
                //saveFileDialog.Filter = "PPM Files (.ppm)|*.ppm";
                //
                //bool? result = saveFileDialog.ShowDialog();
                //
                //string path = saveFileDialog.FileName;
                //
                //if (path != null) {
                //    FileStream outfile = new FileStream(@$"{path}", FileMode.Create);     //TODO
                //
                //    string buffer = "";
                //
                //    for (int line = 0; line < publicEncryptedPPM.Length; line++) {
                //        buffer += publicEncryptedPPM[line];
                //    }
                //
                //    char[] bufferChars = buffer.ToCharArray();
                //
                //    for (int i = 0; i < bufferChars.Length; i++) {
                //        byte data = (byte)bufferChars[i];
                //        outfile.WriteByte(data);
                //    }
                //    outfile.Close();
                //}
            }
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


        #endregion

        #region ReadPPM

        private string[] GetPPMData(string path) {
            bool parser;

            string[] PPMdata = LoadArray(path);

            return PPMdata;
        }

        private void BuildPPM(string[] PPMdata) {
            bool parser;

            string fileType = PPMdata[0];

            List<byte[]> RGBvalues = new List<byte[]>();

            if ((fileType != "P3" && fileType != "P6") || PPMdata.Length < 5) {
                CharOflow.Content = "Invalid file format";
                CharOflow.Foreground = Brushes.Red;
     
            } else {

                string comment = PPMdata[1];
                string[] imgRes = PPMdata[2].Split(" ");
                string RGBchannel = PPMdata[3];

                string resWidthStr = imgRes[0];
                string resHeightStr = imgRes[1];
                int resHeight;
                int resWidth;

                parser = int.TryParse(resHeightStr, out resHeight);
                parser = int.TryParse(resWidthStr, out resWidth);

                if (fileType == "P3") {
                    RGBvalues = ReadP3(PPMdata);

                } else if (fileType == "P6") {
                    RGBvalues = ReadP6(PPMdata);
                }

                PPMbitmap = BuildBitmap(resHeight, resWidth, RGBvalues, PPMbitmap);
                DisplayBitmap(PPMbitmap);
            }
        }

        private List<byte[]> ReadP3(string[] PPMdata) {
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

        private List<byte[]> ReadP6(string[] PPMdata) {
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

        #endregion

        #region Bitmap

        private BitmapMaker BuildBitmap(int resHeight, int resWidth, List<byte[]> RGBvalues, BitmapMaker PPMbitmap) {

            PPMbitmap = new BitmapMaker(resWidth, resHeight);

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

        private void DisplayBitmap(BitmapMaker PPMbitmap) {
            WriteableBitmap wbmImage = PPMbitmap.MakeBitmap();
            imgMain.Source = wbmImage;
        }

        private string[] BitmapToP3(BitmapMaker encryptedBitmap) {

            string[] encryptedPPM = new string[((encryptedBitmap.Height * encryptedBitmap.Width) * 3) + 4];

            encryptedPPM[0] = "P3\n";
            encryptedPPM[1] = "# File created with Koraku's PPM encryption software.\n";
            encryptedPPM[2] = $"{encryptedBitmap.Width} {encryptedBitmap.Height}\n";
            encryptedPPM[3] = "255\n";

            int y = 0;
            int x = 0;

            int line = 4;

            for (y = y; y < encryptedBitmap.Height; y++) {
                if (x == encryptedBitmap.Width) {
                    x = 0;
                }
                for (x = x; x < encryptedBitmap.Width; x++) {

                    byte[] RGB = encryptedBitmap.GetPixelData(x, y);

                    int writeLine = line;

                    encryptedPPM[writeLine] = $"{RGB[0]}\n";
                    encryptedPPM[writeLine+=1] = $"{RGB[1]}\n";
                    encryptedPPM[writeLine+=1] = $"{RGB[2]}\n";

                    line += 3;
                }
            }
            return encryptedPPM;
        }

        private string[] BitmapToP6(BitmapMaker encryptedBitmap) {
            string[] placeholder = {};
            //TODO
            return placeholder;
        }

        #endregion

        #region Encryption

        private char[] BuildChars() {
            char[] encryptionChars = { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', ' ', '.', '!', '?', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };   //if byte / i == wholeNum
            char[] encryption = new char[256];

            int j = 0;

            for (int i = 0; i < encryption.Length; i++) {

                    encryption[i] = encryptionChars[j];

                if (j == encryptionChars.Count() - 1) {
                    j = 0;
                } else {
                    j++;
                }
            }
            return encryption;
        }

        private BitmapMaker EncryptMessage(BitmapMaker PPMbitmap) {

            string message = TxtBoxMessage.Text.ToUpper();

            BitmapMaker encryptedBitmap = PPMbitmap;

            char[] encryptionChars = BuildChars();

            double yInc = PPMbitmap.Height / 16;
            double xInc = PPMbitmap.Width / 16;

            yInc = Math.Floor(yInc);
            xInc = Math.Floor(xInc);

            int xStart = 0;

            int y = 0;
            int x = 0;

            for (int msgChar = 0; msgChar < message.Length; msgChar++) {
                char letter = message[msgChar];

                x += (int)xInc;

                for (x = xStart; x < PPMbitmap.Width; x += 0) {
                    x += (int)xInc;

                    if (x >= PPMbitmap.Width) {
                        x = 0;
                        y += (int)yInc;
                    }

                    byte[] pixelData = PPMbitmap.GetPixelData(x, y);
                    int rVal = pixelData[0];                           //get red pixel (adjustment pixel)

                    for (int encVal = rVal; encVal < 256; encVal++) {
                        if (letter == encryptionChars[encVal]) {
                            pixelData[0] = (byte)encVal;

                            byte[] RGBpixel = { pixelData[0], pixelData[1], pixelData[2] };
                            encryptedBitmap.SetPixel(x, y, RGBpixel[0], RGBpixel[1], RGBpixel[2]);

                            xStart = x;

                            break;

                        } else {
                            if (encVal == 255) {
                                encVal = 214;
                            } else if (encryptionChars.Contains(letter) == false) {
                                break;
                            }
                        }
                    }
                    break;
                }
            }
            return encryptedBitmap;
        }

        #endregion

        #region GUI / XAML

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
            } else if (imgMain.Source == null) {
                CharOflow.Content = "No file selected";
                CharOflow.Foreground = Brushes.Red;
                return;
            } else if (TxtBoxMessage.Text.Length == 0) {
                CharOflow.Content = "No message entered";
                CharOflow.Foreground = Brushes.Red;
                return;

            } else {
                HideOflowLabel();

                List<byte[]> RGBvalues = new List<byte[]>();

                string[] PPMdata = GetPPMData(globalPath);

                double imgHeight = imgMain.Source.Height;
                double imgWidth = imgMain.Source.Width;

                string fileType = PPMdata[0];

                if (imgHeight < 16 || imgWidth < 16) {
                    CharOflow.Content = "Image must be 16x16 or greater";
                    CharOflow.Foreground = Brushes.Red;
                } else {

                    BitmapMaker encryptedBitmap = EncryptMessage(PPMbitmap);
                    imgMain.Source = encryptedBitmap.MakeBitmap();
                    publicEncryptedBitmap = encryptedBitmap;

                    //publicEncryptedPPM = BitmapToP3(encryptedBitmap);
                }
            }
        }

        private void HideOflowLabel() {
            var converter = new System.Windows.Media.BrushConverter();
            var bgColor = (Brush)converter.ConvertFromString("#FFFFD8A8");

            CharOflow.Foreground = bgColor;
        }

        #endregion
    }
}