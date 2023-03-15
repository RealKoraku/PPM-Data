﻿using System;
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
using static System.Net.WebRequestMethods;

namespace ImageLoaderMessage {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
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

                //CALL LOADIMAGE METHOD
                List<byte[]> RGBvalues = new List<byte[]>();

                GetPPMData(selectedFile);
            }
        }

        private void GetPPMData(string path) {

            bool parser;

            string[] PPMdata = Load(path);

            string fileType = PPMdata[0];
            string comment = PPMdata[1];
            string imgRes = PPMdata[2];
            string RGBchannel = PPMdata[3];

            char[] res = imgRes.ToCharArray();
            string resHeightStr = "";
            string resWidthStr = "";
            int resHeight;
            int resWidth;

            bool resSet = false;

            for (int i = 0; i < res.Length; i++) {
                if (res[i] != ' ' && resSet == false) {
                    resHeightStr += res[i];
                } else if (res[i] == ' ') {
                    resSet = true;
                } else {
                    resWidthStr += res[i];
                }
            }

            parser = int.TryParse(resHeightStr, out resHeight);
            parser = int.TryParse(resWidthStr, out resWidth);

            byte[] RGB = new byte[3];
            int RGBline = 4;
            int startPixel = 4;

            List<byte[]> RGBvalues = new List<byte[]>();

            for (int line = 4; line < PPMdata.Length - 1; line++) {

                for (int rgb = 0; rgb < 3; rgb++) {

                    parser = byte.TryParse(PPMdata[line], out RGB[rgb]);
                    line++;

                    if (rgb == 2) {
                        RGBvalues.Add(RGB);
                    }
                }
            }

            BuildBitmap(resHeight, resWidth, PPMdata, RGBvalues);
        }

        private string[] Load(string path) {
            string[] lines;
            string data = "";
            string[] records;

            path = @"C:\Users\MCA\source\repos\Bitmap\mario_binary.ppm";

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

        private void BuildBitmap(int resHeight, int resWidth, string[] PPMdata, List<byte[]> RGBvalues) {

            BitmapMaker PPMbitmap = new BitmapMaker(resWidth, resHeight);

            int RGBvalIndex = 0;

            for (int y = 0; y < resHeight; y++) {
                for (int x = 0; x < resWidth; x++) {

                    byte RGBr = RGBvalues[RGBvalIndex][0];
                    byte RGBg = RGBvalues[RGBvalIndex][1];
                    byte RGBb = RGBvalues[RGBvalIndex][2];
                    PPMbitmap.SetPixel(x, y, RGBr, RGBg, RGBb);

                    RGBvalIndex += 3;
                }
            }
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

            var converter = new System.Windows.Media.BrushConverter();
            var bgColor = (Brush)converter.ConvertFromString("#FFFFD8A8");

            if (TxtBoxMessage.Text.Length > 255) {
                CharOflow.Foreground = Brushes.Red;
                return;
            } else {
                CharOflow.Foreground = bgColor;
            }
        }
    }
}