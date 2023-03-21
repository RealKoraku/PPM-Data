using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

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
            HideOflowLabel();
        }

        #region File dialogs / Open / Save

        private void muiOpenPPM_Click(object sender, RoutedEventArgs e) {     

            OpenFileDialog openFileDialog = new OpenFileDialog();       //create open file dialog

            openFileDialog.DefaultExt = ".PPM";                         //filter to ppm files
            openFileDialog.Filter = "PPM Files (.ppm)|*.ppm";

            bool? result = openFileDialog.ShowDialog();                 //open dialog

            if (result == true) {
                string selectedFile = openFileDialog.FileName;          //store file name

                globalPath = selectedFile;

                List<byte[]> RGBvalues = new List<byte[]>();

                string[] PPMdata = GetPPMData(selectedFile);            //convert ppm text file to string array
                BuildPPM(PPMdata);
            }
        }

        private void SavePPM() {
            SaveFileDialog saveFileDialog = new SaveFileDialog();   //create save dialog

            saveFileDialog.DefaultExt = ".PPM";                     //filter to ppm
            saveFileDialog.Filter = "PPM Files (.ppm)|*.ppm";

            bool? result = saveFileDialog.ShowDialog();             //open dialog

            string path = saveFileDialog.FileName;

            if (path != null) {
                FileStream outfile = new FileStream(@$"{path}", FileMode.Create);   //create file at selected path

                string buffer = "";

                for (int line = 0; line < publicEncryptedPPM.Length; line++) {      //for each line of text in ppm
                    buffer += publicEncryptedPPM[line];                             //add to buffer string
                }

                char[] bufferChars = buffer.ToCharArray();                          //convert string to char array

                for (int i = 0; i < bufferChars.Length; i++) {                      //write each char to selected file
                    byte data = (byte)bufferChars[i];
                    outfile.WriteByte(data);
                }
                outfile.Close();                                                    //close file
            }
        }

        #endregion

        #region ReadPPM

        private void BuildPPM(string[] PPMdata) {
            bool parser;

            string fileType = PPMdata[0];                                           //first line is P3 or P6

            List<byte[]> RGBvalues = new List<byte[]>();

            if ((fileType != "P3" && fileType != "P6") || PPMdata.Length < 5) {     //if not P3 or p6, or length < 5 (no pixel data)
                CharOflow.Content = "Invalid file format";
                ShowOflowLabel();                                                   //throw error
     
            } else {

                string[] comment = GetComments(PPMdata);                            //gather comments into array

                int line = comment.Length + 1;                                      //line after comments

                string[] imgRes = PPMdata[line].Split(" ");                         //gather resolution info
                string RGBchannel = PPMdata[line+=1];                               //RGB max channel next line

                int resHeight;
                int resWidth;

                parser = int.TryParse(imgRes[0], out resHeight);                    //split height and width
                parser = int.TryParse(imgRes[1], out resWidth);

                if (fileType == "P3") {                                             //determine P3 or P6
                    RGBvalues = ReadP3(PPMdata);                                    //populate RGB values

                } else if (fileType == "P6") {
                    RGBvalues = ReadP6(PPMdata);
                }

                PPMbitmap = BuildBitmap(resHeight, resWidth, RGBvalues, PPMbitmap);    //build bitmap
                DisplayBitmap(PPMbitmap);                                              //display constructed image
            }
        }

        private string[] GetComments(string[] PPMdata) {
            int comments = 0;

            for (int commentLine = 1; commentLine < PPMdata.Length; commentLine++) {
                if (PPMdata[commentLine][0] == '#') {
                    comments++;
                } else {
                    break;
                }
            }
            string[] comment = new string[comments];

            for (int line = 1; line <= comment.Length; line++) {
                comment[line-1] = PPMdata[line];
            }
            return comment;
        }

        private List<byte[]> ReadP3(string[] PPMdata) {                     //Read P3
            bool parser;
            string[] comment = GetComments(PPMdata);

            List<byte[]> RGBvalues = new List<byte[]>();

            HideOflowLabel();

            for (int line = comment.Length +3; line < PPMdata.Length - 1; line += 0) {      //for each line in ppm text, starting after header data

                byte[] RGB = new byte[3];                                   //byte array for RGB data
                byte RGBbyte = 0;                                           //single byte for R/G/B

                for (int rgb = 0; rgb < RGB.Length; rgb++) {                //for length of RGB data

                    parser = byte.TryParse(PPMdata[line], out RGBbyte);     //convert that line into a byte
                    RGB[rgb] = RGBbyte;                                     //set current R/G/B position to that byte
                    line++;                                                 //go to next line    
                }

                RGBvalues.Add(RGB);                                         //once pixel is populated, add to RGBvalues list
            }
            return RGBvalues;
        }

        private List<byte[]> ReadP6(string[] PPMdata) {                     //Read P6
            bool parser;
            string[] comment = GetComments(PPMdata);

            List<byte[]> RGBvalues = new List<byte[]>();

            HideOflowLabel();

            char[] binaryData = PPMdata[comment.Length +3].ToCharArray();   //store binary data chars of ppm text to char array

            for (int bytes = 0; bytes < binaryData.Length; bytes+= 0) {     //for each character in the binary data array

                byte[] RGB = new byte[3];                                   //byte array for RGB data
                byte RGBbyte = 0;                                           //single byte for R/G/B

                for (int rgb = 0; rgb < RGB.Length; rgb++) {                //for length of RGB array             

                    RGBbyte = (byte)binaryData[bytes];                      //cast current char to byte, store in R/G/B byte
                    RGB[rgb] = RGBbyte;                                     //set current R/G/B to that byte
                    bytes++;                                                //go to next char in binary data
                }
                RGBvalues.Add(RGB);                                         //once pixel is populated, add to RGBvalues list
            }
            return RGBvalues;
        }

        private string[] GetPPMData(string path) {
            string[] lines;
            string data = "";
            string[] records;

            FileStream inFile = new FileStream(path, FileMode.Open);    //read data from specified file

            while (inFile.Position < inFile.Length) {                   
                data += (char)inFile.ReadByte();                        //add each char from text to data string
            }//end while

            inFile.Close();                                             //close file

            lines = data.Split("\n");                                   //split lines into string array

            return lines;
        }

        #endregion

        #region Bitmap

        private BitmapMaker BuildBitmap(int resHeight, int resWidth, List<byte[]> RGBvalues, BitmapMaker PPMbitmap) {

            PPMbitmap = new BitmapMaker(resWidth, resHeight);       //create Bitmap of specified height and width

            int RGBvalIndex = 0;              

            for (int y = 0; y < resHeight; y++) {                   //scan bitmap x, y
                for (int x = 0; x < resWidth; x++) {

                    byte RGBr = RGBvalues[RGBvalIndex][0];          //red value is equal to the 1st byte in array of RGBvalIndex
                    byte RGBg = RGBvalues[RGBvalIndex][1];          //blue value is equal to 2nd byte in array
                    byte RGBb = RGBvalues[RGBvalIndex][2];          //green = 3rd byte
                    PPMbitmap.SetPixel(x, y, RGBr, RGBg, RGBb);     //set constructed pixel to current position

                    RGBvalIndex++;                                  //next item in RGBvalues list
                }
            }

            return PPMbitmap;
        }

        private void DisplayBitmap(BitmapMaker PPMbitmap) {
            WriteableBitmap wbmImage = PPMbitmap.MakeBitmap();      //PPMbitmap to writeable bitmap
            imgMain.Source = wbmImage;                              //set image box source to writeable bitmap
        }

        private string[] BitmapToP3(BitmapMaker encryptedBitmap) {

            string[] encryptedPPM = new string[((encryptedBitmap.Height * encryptedBitmap.Width) * 3) + 4];     //encrypted P3 ppm length will be the resolution * 3 (3 lines for each pixel) plus four (4 lines of header data)

            encryptedPPM[0] = "P3\n";                                                                           //set file type
            encryptedPPM[1] = "# File created with Koraku's PPM encryption software.\n";                        //set comment
            encryptedPPM[2] = $"{encryptedBitmap.Width} {encryptedBitmap.Height}\n";                            //set resolution
            encryptedPPM[3] = "255\n";                                                                          //set RGB channel

            int y = 0;
            int x = 0;

            int line = 4;                                               //start at line 4 of ppm

            for (y = y; y < encryptedBitmap.Height; y++) {              //scan bitmap height
                if (x == encryptedBitmap.Width) {                       //if x reaches width of bitmap
                    x = 0;                                              //go back to leftmost position
                }
                for (x = x; x < encryptedBitmap.Width; x++) {           //scan width

                    byte[] RGB = encryptedBitmap.GetPixelData(x, y);    //byte array for current pixel in bitmap                           

                    int writeLine = line;                               //line placeholder

                    encryptedPPM[writeLine] = $"{RGB[0]}\n";            //write that line of ppm text to 1st rgb value of pixel
                    encryptedPPM[writeLine+=1] = $"{RGB[1]}\n";         //write next line to 2nd rgb value
                    encryptedPPM[writeLine+=1] = $"{RGB[2]}\n";         //3rd line to 3rd

                    line += 3;                                          //progress 3 lines
                }
            }
            return encryptedPPM;
        }

        private string[] BitmapToP6(BitmapMaker encryptedBitmap) {                          //Bitmap to P6
            string[] encryptedPPM = new string[5];                                          //P6 will only be 5 lines

            encryptedPPM[0] = "P6\n";                                                       //similar process to P3
            encryptedPPM[1] = "# File created with Koraku's PPM encryption software.\n";
            encryptedPPM[2] = $"{encryptedBitmap.Width} {encryptedBitmap.Height}\n";
            encryptedPPM[3] = "255\n";
            encryptedPPM[4] = "";

            int y = 0;
            int x = 0;

            for (y = y; y < encryptedBitmap.Height; y++) {
                if (x == encryptedBitmap.Width) {
                    x = 0;
                }
                for (x = x; x < encryptedBitmap.Width; x++) {

                    byte[] RGB = encryptedBitmap.GetPixelData(x, y);                        //similar to p3, but each R/G/B byte value is cast to a char and added to line 5 of the ppm text

                    char P6R = (char)RGB[0];
                    char P6G = (char)RGB[1];
                    char P6B = (char)RGB[2];

                    encryptedPPM[4] += $"{P6R}";
                    encryptedPPM[4] += $"{P6G}";
                    encryptedPPM[4] += $"{P6B}";
                }
            }
            return encryptedPPM;
        }

        #endregion

        #region Encryption

        private char[] BuildChars() {               //create array of A-Z, 0-9, and some punctuation, repeating until it reaches 256
            char[] encryptionChars = { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', ' ', '.', ',', '!', '?', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };   //if byte / i == wholeNum
            char[] encryption = new char[256];

            int j = 0;

            for (int i = 0; i < encryption.Length; i++) {

                    encryption[i] = encryptionChars[j];     //populate array with repeating loop

                if (j == encryptionChars.Count() - 1) {
                    j = 0;
                } else {
                    j++;
                }
            }
            return encryption;
        }

        private BitmapMaker EncryptMessage(BitmapMaker PPMbitmap) {

            string message = TxtBoxMessage.Text.ToUpper();  //grab encryption box text, convert to upper

            BitmapMaker encryptedBitmap = PPMbitmap;        //create new Bitmap equal to original

            char[] encryptionChars = BuildChars();          //populate encryption array

            double yInc = PPMbitmap.Height / 16;            //which pixels to change
            double xInc = PPMbitmap.Width / 16;

            yInc = Math.Floor(yInc);                        //always round decimal down
            xInc = Math.Floor(xInc);

            int xStart = 0;                                 

            int y = 0;                                      //x, y start at 0 (topleft pixel)
            int x = 0;

            for (int msgChar = 0; msgChar < message.Length; msgChar++) {                            //for each letter in the encryption message
                char letter = message[msgChar];                                                     //current letter


                for (x = xStart; x < PPMbitmap.Width; x += 0) {                                     //scan width of bitmap
                    x += (int)xInc;                                                                 //go to next x position

                    if (x >= PPMbitmap.Width) {                                                     //if it reaches the end                 
                        x = 0;                                                                      //go back to leftmost pixel                     
                        y += (int)yInc;                                                             //go to next y position
                    }

                    byte[] pixelData = PPMbitmap.GetPixelData(x, y);                                //get pixel data of current pixel

                    int modVal = pixelData[2];                                                      //isolate R/G/B value (for encrypting) 
                    int RGBIndex = 2;

                    int[] RGBinfo = DecideRGBValue(modVal, pixelData, RGBIndex);                    //decide if R, G, or B

                    modVal = RGBinfo[0];                                                            //separate values
                    RGBIndex = RGBinfo[1];

                    for (int encVal = modVal; encVal < 256; encVal++) {                             //encVal for rgb value / encryption value                  
                        if (letter == encryptionChars[encVal]) {                                    //if selected letter is equal to that letter of the array  
                            pixelData[RGBIndex] = (byte)encVal;                                     //set the current pixel RGBIndex to the correpsonding value

                            byte[] RGBpixel = { pixelData[0], pixelData[1], pixelData[2] };         //reconstructed pixel data
                            encryptedBitmap.SetPixel(x, y, RGBpixel[0], RGBpixel[1], RGBpixel[2]);  //set that current pixel to the new pixel data

                            xStart = x;                                                             //remember where x left off

                            break;

                        } else {
                            if (encVal == 255) {                                                    //if it reaches the end and no value was found
                                encVal = 213;                                                       //go back about one iteration of the enc char loop
                            } else if (encryptionChars.Contains(letter) == false) {                 //if its not even in the array at all
                                break;                                                              //break/skip/to be announced
                            }
                        }
                    }
                    break;
                }
            }
            return encryptedBitmap;
        }

        private int[] DecideRGBValue(int modVal, byte[] pixelData, int RGBIndex) {
            int[] RGBinfo = new int[2];

            if (modVal == pixelData[0]) {
                modVal = pixelData[1];
                RGBIndex = 1;

            } else if (modVal == pixelData[1]) {
                modVal = pixelData[2];
                RGBIndex = 2;

            } else {
                modVal = pixelData[0];
                RGBIndex = 0;
            }

            RGBinfo[0] = modVal;
            RGBinfo[1] = RGBIndex;
            return RGBinfo;
        }

        #endregion

        #region GUI / XAML

            private void TxtBoxMessage_TextChanged(object sender, TextChangedEventArgs e) {
            string message;

            var converter = new System.Windows.Media.BrushConverter();
            var brushesCustomTeal = (Brush)converter.ConvertFromString("#FF3ABFB6");

            if (TxtBoxMessage.Text.Length > 255) {
                TxtBoxMessage.Foreground = Brushes.Red;
            } else {
                TxtBoxMessage.Foreground = brushesCustomTeal;
            }

            message = TxtBoxMessage.Text;
        }

        private void muiSaveP3_Click(object sender, RoutedEventArgs e) {
            if (imgMain.Source == null) {                              //if no image selected 
                return;                                                //cancel operation
            } else if (publicEncryptedBitmap == null) {                //if no bitmap encrypted
                CharOflow.Content = "Encrypt file first";              //show error
                ShowOflowLabel();
                return;                                                //cancel operation

            } else {

                publicEncryptedPPM = BitmapToP3(publicEncryptedBitmap);  //convert public encrypted ppm to p3

                HideOflowLabel();

                SavePPM();
            }
        }

        private void muiSaveP6_Click(object sender, RoutedEventArgs e) {                //similar process as P3
            if (imgMain.Source == null) {
                return;
            } else if (publicEncryptedBitmap == null) {
                CharOflow.Content = "Encrypt file first";
                ShowOflowLabel();
                return;

            } else {

                publicEncryptedPPM = BitmapToP6(publicEncryptedBitmap);

                SavePPM();
            }
        }


        private void BtnEncrypt_Click(object sender, RoutedEventArgs e) {
            if (TxtBoxMessage.Text.Length > 255) {
                CharOflow.Content = "Too many chars!";
                ShowOflowLabel();
                return;
            } else if (imgMain.Source == null) {
                CharOflow.Content = "No file selected";
                ShowOflowLabel();
                return;
            } else if (TxtBoxMessage.Text.Length == 0) {
                CharOflow.Content = "No message entered";
                ShowOflowLabel();
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
                    ShowOflowLabel();
                } else {

                    BitmapMaker encryptedBitmap = EncryptMessage(PPMbitmap);
                    imgMain.Source = encryptedBitmap.MakeBitmap();
                    publicEncryptedBitmap = encryptedBitmap;
                }
            }
        }

        private void HideOflowLabel() {
            CharOflow.Foreground.Opacity = 0;
        }

        private void ShowOflowLabel() {
            CharOflow.Foreground.Opacity = 100;
        }
        #endregion
    }
}