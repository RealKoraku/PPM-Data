using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Printing;
using System.Text;
using System.Windows;
using System.Windows.Automation.Peers;
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
        public BitmapMaker publicPPMbitmap = new BitmapMaker(0, 0);
        public BitmapMaker publicEncryptedBitmap;
        public string[] publicHeader;
        public string[] publicFileComments;
        public string[] publicEncryptedPPM;   

        public MainWindow() {
            InitializeComponent();
            HideOflowLabel();
        }

        #region File dialogs / Open / Save

        private void muiOpenPPM_Click(object sender, RoutedEventArgs e) {
            bool parser;

            OpenFileDialog openFileDialog = new OpenFileDialog();       //create open file dialog

            openFileDialog.DefaultExt = ".PPM";                         //filter to ppm files
            openFileDialog.Filter = "PPM Files (.ppm)|*.ppm";

            bool? result = openFileDialog.ShowDialog();                 //open dialog

            if (result == true) {
                string selectedFile = openFileDialog.FileName;          //store file name

                globalPath = selectedFile;

                string PPMtype = DetermineHeader(selectedFile);

                List<byte[]> RGBvalues = new List<byte[]>();

                if (PPMtype != "P3" && PPMtype != "P6") {
                    CharOflow.Content = "Invalid file format";
                    ShowOflowLabel();

                } else {

                    if (PPMtype == "P3") {

                        string[] PPMdata = GetP3Data(selectedFile);            
                        string[] header = BuildHeader(PPMdata);
                        publicHeader = header;

                        RGBvalues = ReadP3(PPMdata);

                    } else if (PPMtype == "P6") {

                        List<byte> binPPMdata = GetP6Data(selectedFile);
                        string[] header = BuildP6Header(globalPath);
                        publicHeader = header;

                        RGBvalues = ReadP6(binPPMdata);
                    }

                    string[] imgRes = publicHeader[publicFileComments.Length + 1].Split(" ");

                    parser = int.TryParse(imgRes[0], out int imgWidth);                    //split height and width
                    parser = int.TryParse(imgRes[1], out int imgHeight);

                    BitmapMaker imageBmp = BuildBitmap(imgHeight, imgWidth, RGBvalues);
                    DisplayBitmap(imageBmp);
                    publicPPMbitmap = imageBmp;
                }
            }
        }

        private string DetermineHeader(string path) {       //grab first line of file
            StreamReader inFile = new StreamReader(path);
            string topLine = inFile.ReadLine();
            inFile.Close();

            return topLine;
        }

        private string[] GetP3Data(string path) {
            string[] lines;
            string data = "";
            string[] records;

            FileStream inFile = new FileStream(path, FileMode.Open);    //read data from specified file

            StringBuilder dataSB = new StringBuilder("");

            while (inFile.Position < inFile.Length) {
                int dataByte = inFile.ReadByte();
                char dataChar = (char)dataByte;
                dataSB.Append(dataChar);
            }

            data = dataSB.ToString();

            lines = data.Split("\n");                                   //split lines into string array

            inFile.Close();
            return lines;
        }

        private string SaveFileDialog() {
            SaveFileDialog saveFileDialog = new SaveFileDialog();   //create save dialog

            saveFileDialog.DefaultExt = ".PPM";                     //filter to ppm
            saveFileDialog.Filter = "PPM Files (.ppm)|*.ppm";

            bool? result = saveFileDialog.ShowDialog();             //open dialog

            string path = saveFileDialog.FileName;

            return path;
        }

        private void SaveP3() {
            string path = SaveFileDialog();
            FileStream outfile = new FileStream(@$"{path}", FileMode.Create);   //create file at selected path

            string buffer = "";

            StringBuilder dataSB = new StringBuilder("");

            for (int line = 0; line < publicEncryptedPPM.Length; line++) {      //for each line of text in ppm
                dataSB.Append(publicEncryptedPPM[line]);                        //add to buffer string
            }

            buffer = dataSB.ToString();

            char[] bufferChars = buffer.ToCharArray();                          //convert string to char array

            for (int i = 0; i < bufferChars.Length; i++) {                      //write each char to selected file
                byte data = (byte)bufferChars[i];
                outfile.WriteByte(data);
            }
            outfile.Close();                                                    //close file
        }

        private List<byte> GetP6Data(string path) {
            List<byte> fileData = new List<byte>();

            FileStream infile = new FileStream(path, FileMode.Open);

            while (infile.Position < infile.Length) {
                int byteInt = infile.ReadByte();
                byte byteData = (byte)byteInt;
                fileData.Add(byteData);
            }
            infile.Close();
            return fileData;
        }

        private void SaveP6(List<byte> lstToFile) {
            string path = SaveFileDialog();

            FileStream outfile = new FileStream(@$"{path}", FileMode.Create);   //create file at selected path

            byte[] outData = lstToFile.ToArray();

            for (int bytes = 0; bytes < outData.Length; bytes++) {
                outfile.WriteByte(outData[bytes]);
            }

            outfile.Close();                                                    //close file
        }
        

        #endregion

        #region ReadPPM

        private string[] BuildHeader(string[] PPMdata) {

            bool parser;

            string fileType = PPMdata[0];                                       //first line is P3 or P6

            List<byte[]> RGBvalues = new List<byte[]>();

            string[] comment = GetComments(PPMdata);                            //gather comments into array
            publicFileComments = comment;

            string[] headerLines = new string[comment.Length + 3];
            int line = comment.Length + 1;                                      //line after comments

            string[] imgRes = PPMdata[line].Split(" ");                         //gather resolution info
            string RGBchannel = PPMdata[line += 1];                             //RGB max channel next line

            int resHeight;
            int resWidth;

            parser = int.TryParse(imgRes[0], out resWidth);                     //split height and width
            parser = int.TryParse(imgRes[1], out resHeight);

            headerLines = CondenseHeader(fileType, comment, imgRes, RGBchannel);

            return headerLines;
        }

        private string[] GetComments(string[] PPMdata) {
            int comments = 0;

            for (int commentLine = 1; commentLine < PPMdata.Length; commentLine++) {
                if (PPMdata[commentLine][0] == '#') {                                   //comments always start with #
                    comments++;
                } else {
                    break;
                }
            }
            string[] comment = new string[comments];

            for (int line = 1; line < comments + 1; line++) {
                comment[line - 1] = PPMdata[line];
            }

            return comment;
        }

        private string[] CondenseHeader(string fileType, string[] comments, string[] imgRes, string channel) {
            string[] headerData = new string[comments.Length + 3];

            headerData[0] = fileType;

            int headerLine = 1;
            for (int line = 0; line < comments.Length; line += 0) {
                headerData[headerLine] = comments[line];
                line++;
                headerLine = line + 1;
            }

            headerData[headerLine] = $"{imgRes[0]} {imgRes[1]}";
            headerLine++;
            headerData[headerLine] = channel;
            return headerData;
        }

        private List<byte[]> ReadP3(string[] PPMdata) {                     //Read P3
            bool parser;
            string[] comment = publicFileComments;

            List<byte[]> RGBvalues = new List<byte[]>();

            HideOflowLabel();

            for (int line = comment.Length + 3; line < PPMdata.Length - 1; line += 0) {      //for each line in ppm text, starting after header data

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

        private string[] BuildP6Header(string path) {
            string[] PPMdata;
            string data = "";

            FileStream inFile = new FileStream(path, FileMode.Open);    //read data from specified file

            StringBuilder dataSB = new StringBuilder("");

            while (inFile.Position < inFile.Length) {
                int dataByte = inFile.ReadByte();
                char dataChar = (char)dataByte;
                dataSB.Append(dataChar);
            }
            inFile.Close();

            data = dataSB.ToString();
            PPMdata = data.Split("\n");                                   //split lines into string array

            string[] headerLines = BuildHeader(PPMdata);

            return headerLines;
        }

        private List<byte[]> ReadP6(List<byte> PPMdata) {                     //Read P6
            bool parser;

            HideOflowLabel();

            List<byte> headerBytes = new List<byte>();                        //store header as byte array
            List<byte> byteValues = new List<byte>();                         //store binary RGB as byte array

            bool headerComplete = false;                                    
            int lineCount = 0;                                                //count new lines
            
            for (int val = 0; headerComplete == false; val++) {               //scan PPMdata until headerComplete = true
                if (PPMdata[val] == 10) {                                     //if that byte == LineFeed
                    lineCount++;                                              //add to line count
                }
                headerBytes.Add(PPMdata[val]);                                //add byte to headerBytes

                if (lineCount == publicFileComments.Length + 3) {             //total amount of header lines
                    headerComplete = true;                                    //header is complete
                }
            }

            List<byte[]> RGBvalues = new List<byte[]>();

            for (int i = headerBytes.Count; i < PPMdata.Count; i += 3) {      //populate list with byte arrays (pixels)
                byte[] RGB = new byte[3];

                RGB[0] = PPMdata[i];
               
                RGB[1] = PPMdata[i+1];
               
                RGB[2] = PPMdata[i+2];

                RGBvalues.Add(RGB);
            }
            return RGBvalues;
        }

        #endregion

        #region Bitmap

        private BitmapMaker BuildBitmap(int resHeight, int resWidth, List<byte[]> RGBvalues) {

            BitmapMaker PPMbitmap = new BitmapMaker(resWidth, resHeight);       //create Bitmap of specified height and width

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
            string[] comments = publicFileComments;                        //set comment

            int PPMline = 1;
            for (int i = 0; i < comments.Length; i++) {
                encryptedPPM[PPMline] = comments[i];
            }

            encryptedPPM[comments.Length+1] = $"{encryptedBitmap.Width} {encryptedBitmap.Height}\n";                            //set resolution
            encryptedPPM[comments.Length+2] = "255\n";                                                                          //set RGB channel

            int x = 0;
            int line = 4;                                               //start at line 4 of ppm

            for (int y = 0; y < encryptedBitmap.Height; y++) {          //scan bitmap height
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

        private List<byte> BitmapToP6(BitmapMaker encryptedBitmap) {                          //Bitmap to P6
            StringBuilder dataSB = new StringBuilder("");

            List<byte> lstToFile = new List<byte>();

            string headerData = $"P6\n";
            string[] comments = publicFileComments;

            for (int i = 0; i < comments.Length; i++) {
                headerData += $"{comments[i]}\n";
            }
            headerData += $"{encryptedBitmap.Width} {encryptedBitmap.Height}\n255\n";       //similar process to P3

            for (int i = 0; i < headerData.Length; i++) {
                lstToFile.Add((byte)headerData[i]);
            }

            int x = 0;

            for (int y = 0; y < encryptedBitmap.Height; y++) {
                if (x == encryptedBitmap.Width) {
                    x = 0;
                }
                for (x = x; x < encryptedBitmap.Width; x++) {

                    byte[] RGB = encryptedBitmap.GetPixelData(x, y);                        //similar to p3, but each R/G/B byte value is cast to a char and added to line 5 of the ppm text
                    
                    for (int RGBval = 0; RGBval < 3; RGBval++) {
                        lstToFile.Add(RGB[RGBval]);
                    }
                }
            }
            return lstToFile;
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

        private BitmapMaker EncodeMessage(BitmapMaker PPMbitmap) {

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

            int RGBIndex = 2;

            HideMsgLength(message, encryptedBitmap);

            for (int msgChar = 0; msgChar < message.Length; msgChar++) {                            //for each letter in the encryption message
                char letter = message[msgChar];                                                     //current letter

                for (x = xStart; x < PPMbitmap.Width; x += 0) {                                     //scan width of bitmap
                    x += (int)xInc;                                                                 //go to next x position

                    if (x >= PPMbitmap.Width) {                                                     //if it reaches the end                 
                        x = 0;                                                                      //go back to leftmost pixel                     
                        y += (int)yInc;                                                             //go to next y position
                    }

                    byte[] pixelData = PPMbitmap.GetPixelData(x, y);                                //get pixel data of current pixel

                    RGBIndex = DecideRGBValue(RGBIndex);                                            //decide if R, G, or B
                                                                                                    
                    int modVal = pixelData[RGBIndex];                                               //assign RGB to be modified

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

        private int DecideRGBValue(int RGBIndex) {
            if (RGBIndex == 2) {
                RGBIndex = 0;
            } else { 
                RGBIndex++;
            }
            return RGBIndex;
        }

        private void HideMsgLength(string message, BitmapMaker bitmap) {
            byte msgLength = (byte)message.Length;
            byte[] firstPixel = bitmap.GetPixelData(0, 0);

            bitmap.SetPixel(0, 0, msgLength, firstPixel[1], firstPixel[2]);
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

                SaveP3();
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

                List<byte> lstToFile = BitmapToP6(publicEncryptedBitmap);

                SaveP6(lstToFile);
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

                string[] PPMdata = GetP3Data(globalPath);

                double imgHeight = imgMain.Source.Height;
                double imgWidth = imgMain.Source.Width;

                if (imgHeight < 16 || imgWidth < 16) {
                    CharOflow.Content = "Image must be 16x16 or greater";
                    ShowOflowLabel();
                } else {

                    BitmapMaker encryptedBitmap = EncodeMessage(publicPPMbitmap);
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