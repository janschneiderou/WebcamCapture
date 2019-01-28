/**
 * ****************************************************************************
 * Copyright (C) 2018 Das Deutsche Institut für Internationale Pädagogische Forschung (DIPF)
 * <p/>
 * This library is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * <p/>
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 * <p/>
 * You should have received a copy of the GNU Lesser General Public License
 * along with this library.  If not, see <http://www.gnu.org/licenses/>.
 * <p/>
 * Contributors: Jan Schneider
 * ****************************************************************************
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
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
using Accord.Audio;
using Accord.Audio.Formats;
using Accord.DirectSound;
using Accord.Video;
using Accord.Video.DirectShow;
using Accord.Video.FFMPEG;

namespace WebcamCapture
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        bool isRecording = false;
        VideoCaptureDevice videoSource;
        AudioCaptureDevice AudioSource;
        ConnectorHub.ConnectorHub myConnector;
        System.DateTime sartRecordingTime;
        System.DateTime lastRecordingVideoTime;
        System.DateTime lastRecordingSoundTime;


        VideoFileWriter writer;


        private MemoryStream stream;


        private WaveEncoder encoder;

        Process process;

        private float[] current;

        private int frames;
        private int samples;
        private TimeSpan duration;

        string filename;
        string filenameAudio;
        string filenameCombined;
        int i;

        public MainWindow()
        {
            InitializeComponent();
            i = 0;
            var videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            videoSource = new VideoCaptureDevice(videoDevices[0].MonikerString);
            
            videoSource.NewFrame += VideoSource_NewFrame;
            videoSource.Start();

            initalizeAudioStuff();
            try
            {
                myConnector = new ConnectorHub.ConnectorHub();
                myConnector.Init();
                myConnector.amIvideo = true;
                myConnector.SendReady();
               

                myConnector.StartRecordingEvent += MyConnector_startRecordingEvent;
                myConnector.StopRecordingEvent += MyConnector_stopRecordingEvent;
            }
            catch
            {

            }


            this.Closing += MainWindow_Closing;

        }

        #region audio
        private void initalizeAudioStuff()
        {

            try
            {
                AudioSource = new AudioCaptureDevice();
                AudioDeviceInfo info = null;
                var adc = new AudioDeviceCollection(AudioDeviceCategory.Capture);
                foreach (var ad in adc)
                {
                    string desc = ad.Description;
                    if (desc.IndexOf("Audio") > -1)
                    {
                        info = ad;
                    }
                }
                if (info == null)
                {
                    AudioSource = new AudioCaptureDevice();
                }
                else
                {
                    AudioSource = new AudioCaptureDevice(info);
                }

                //AudioCaptureDevice source = new AudioCaptureDevice();
                AudioSource.DesiredFrameSize = 4096;
                AudioSource.SampleRate = 44100;
                //int sampleRate = 44100;
                //int sampleRate = 22050;

                AudioSource.NewFrame += AudioSource_NewFrame;
               // AudioSource.Format = SampleFormat.Format64BitIeeeFloat;
                AudioSource.AudioSourceError += AudioSource_AudioSourceError;
                // AudioSource.Start();
                int x = 1;
            }
            catch
            {

            }

            // Create buffer for wavechart control
            current = new float[AudioSource.DesiredFrameSize];

            // Create stream to store file
            stream = new MemoryStream();
            encoder = new WaveEncoder(stream);

            frames = 0;
            samples = 0;
            


            // Start
            AudioSource.Start();


        }

        private void AudioSource_AudioSourceError(object sender, AudioSourceErrorEventArgs e)
        {
            int x = 1;
            x++;
        }

        private void AudioSource_NewFrame(object sender, Accord.Audio.NewFrameEventArgs e)
        {

            if (isRecording)
            {
                System.TimeSpan diff1 = DateTime.Now.Subtract(sartRecordingTime);
                if (diff1.Seconds >= 0.0 )
                {
                    //writer.WriteAudioFrame(e.Signal.RawData);
                    e.Signal.CopyTo(current);

                    encoder.Encode(e.Signal);


                    duration += e.Signal.Duration;

                    samples += e.Signal.Samples;
                    frames += e.Signal.Length;
                }

               
            }
        }

        private void doAudioStop()
        {
            // Stops both cases
            if (AudioSource != null)
            {
                // If we were recording
                AudioSource.SignalToStop();
                AudioSource.WaitForStop();
            }


            var fileStream = File.Create(filenameAudio);
            stream.WriteTo(fileStream);
            fileStream.Close();

            // Also zero out the buffers and screen
            Array.Clear(current, 0, current.Length);
 
        }

        #endregion

        #region video

        private void VideoSource_NewFrame(object sender, Accord.Video.NewFrameEventArgs eventArgs)
        {
            Dispatcher.Invoke(() =>
            {
                System.Drawing.Image myCurrentImage = (Bitmap)eventArgs.Frame.Clone();


                BitmapImage bi = new BitmapImage();
                bi.BeginInit();

                MemoryStream ms = new MemoryStream();
                myCurrentImage.Save(ms, ImageFormat.Bmp);
                ms.Seek(0, SeekOrigin.Begin);

                bi.StreamSource = ms;
                bi.EndInit();

                //Using the freeze function to avoid cross thread operations 
                bi.Freeze();
                ImageWebcam.Source = bi;



                if (isRecording)
                {
                    captureFunction(eventArgs.Frame);
                }
            });

        }


        private void captureFunction(System.Drawing.Bitmap bitmap)
        {
            try
            {

                System.TimeSpan diff1 = DateTime.Now.Subtract(sartRecordingTime);
                //if (diff1.Seconds >= 1.0 / 30)
                //{
                    // vf.WriteVideoFrame(bitmap, diff1);
                    writer.WriteVideoFrame(bitmap, diff1);

                //}


            }
            catch (Exception e)
            {
                int x = i;

            }
            i++;
        }

        #endregion


        #region startRecording

        private void MyConnector_startRecordingEvent(object sender)
        {
            Dispatcher.Invoke(() =>
            {
                Button_Click(null, null);
            });
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            isRecording = true;

            buttonStart.Background = System.Windows.Media.Brushes.Bisque;
            sartRecordingTime = DateTime.Now;
            lastRecordingVideoTime = sartRecordingTime;
            lastRecordingSoundTime = sartRecordingTime;
            string time = DateTime.Now.Hour.ToString();
            time = time + "H" + DateTime.Now.Minute.ToString() + "M" + DateTime.Now.Second.ToString() + "S";
            filenameAudio = time + ".wav";
            filenameCombined = time + "c.mp4";
            time = time + ".mp4";
            filename = time;



            int frameRate = 30;
            int bitRate = 400000;
            int audioBitrate = 320000;
            //int audioBitrate = 4096;
            int sampleRate = 44100;
            //int sampleRate = 22050;
            int channels = 1;



            // vf = new VideoFileWriter();
            // vf.Open(time, 640, 480, 30, VideoCodec.MPEG4 );
            duration = new TimeSpan();
            writer = new VideoFileWriter();
            writer.Open(time, 640, 480, frameRate, VideoCodec.Default, bitRate);
            //writer.Open(filename, 640, 480, frameRate, VideoCodec.Default, bitRate,
            //                                    AudioCodec.MP3, audioBitrate, sampleRate, channels);


        }

        #endregion

        #region stopRecording

        private void MyConnector_stopRecordingEvent(object sender)
        {
            Dispatcher.Invoke(() =>
            {
                Button_Click_1(null, null);
            });
        }




        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            doAudioStop();
            isRecording = false;
            // vf.Close();
            try
            {
                writer.Close();
            }
            catch(Exception ess)
            {

            }
            try
            {
                combineFiles();
                filename = filenameCombined;
            }
            catch
            {

            }

            try
            {
                bool a = process.HasExited;
                myConnector.SendTCPAsync(myConnector.SendFile + filename + myConnector.endSendFile);
            }
            catch (Exception x)
            {
                int i = 0;
                i++;
            }

        }

        private void combineFiles()
        {
            // Process.Start("ffmpeg", "-i " + filename + " -i " + filenameAudio + " -c:v copy -c:a aac -strict experimental " + filenameCombined + "");

            string FFmpegFilename;
            string[] text= File.ReadAllLines("FFMPEGLocation.txt");
            FFmpegFilename = text[0];

            process = new Process();
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.FileName = FFmpegFilename;
            // process.StartInfo.FileName = @"C:\FFmpeg\bin\ffmpeg.exe";

            process.StartInfo.Arguments = "-i " + filename + " -i " + filenameAudio + " -c:v copy -c:a aac -strict experimental " + filenameCombined + " -shortest";


            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;

            process.Start();
            process.WaitForExit();


        }

        #endregion







        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            videoSource.SignalToStop();
            try
            {
                myConnector.Close();
            }
            catch
            {

            }
            
        }
    }
}
