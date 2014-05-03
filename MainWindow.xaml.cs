using System;
using System.IO;
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
using System.Windows.Forms;
using System.Text.RegularExpressions;
using Microsoft.Kinect;
using System.Reflection;


namespace TestKeystrokeInput
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private Rect sceneRect;


        private readonly List<Thing> things = new List<Thing>();
        /// <summary>
        /// Width of output drawing
        /// </summary>
        private const float RenderWidth = 640.0f;

        /// <summary>
        /// Height of our output drawing
        /// </summary>
        private const float RenderHeight = 480.0f;

        /// <summary>
        /// Thickness of drawn joint lines
        /// </summary>
        private const double JointThickness = 3;


        // thickness of center point for torso

        private const double TorsoThickness = 4;



        /// <summary>
        /// Thickness of body center ellipse
        /// </summary>
        private const double BodyCenterThickness = 10;



        private const double TrackedPointThickness = 20;


        private const int BubbleThickness = 25;


        /// <summary>
        /// Thickness of clip edge rectangles
        /// </summary>
        private const double ClipBoundsThickness = 10;

        /// <summary>
        /// Brush used to draw skeleton center point
        /// </summary>
        private readonly Brush centerPointBrush = Brushes.Blue;

        private readonly Brush testBrush = Brushes.DarkViolet;


        private readonly Brush pointBrush = Brushes.DarkBlue;

        private readonly Brush PoppedPointBrush = Brushes.Red;


        /// <summary>
        /// Brush used for drawing joints that are currently tracked
        /// </summary>
        private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));

        /// <summary>
        /// Brush used for drawing joints that are currently inferred
        /// </summary>        
        private readonly Brush inferredJointBrush = Brushes.Yellow;

        /// <summary>
        /// Pen used for drawing bones that are currently tracked
        /// </summary>
        private readonly Pen trackedBonePen = new Pen(Brushes.Green, 6);

        /// <summary>
        /// Pen used for drawing bones that are currently inferred
        /// </summary>        
        private readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1);

        /// <summary>
        /// Drawing group for skeleton rendering output
        /// </summary>
        private DrawingGroup drawingGroup;

        /// <summary>
        /// Drawing image that we will display
        /// </summary>
        private DrawingImage imageSource;

        KinectSensor _ks;

        public class Motion
        {
            public static int LeftHandTest = 0;
            public static bool test = false;
            public static bool BubbleTest = false;
            public static int PlottedBubbles = 0;
            public static int PopTest = 0;
            public static bool ThingsPopulated = false;
        }

        public class CirclesPopped
        {
            public static bool Circle1 = false;
            public static bool Circle2 = false;
            public static bool Circle3 = false;
        }

        private void InitializeKinect()
        {

            if (KinectSensor.KinectSensors.Count > 0)
            {

                _ks = KinectSensor.KinectSensors[0];

                if (_ks.Status == KinectStatus.Connected)
                {
                    //_ks.ColorStream.Enable();
                    //_ks.DepthStream.Enable();
                    _ks.SkeletonStream.Enable();

                    _ks.AllFramesReady += _ks_AllFramesReady;
                    this._ks.SkeletonFrameReady += _ks_SkeletonFrameReady;
                    _ks.Start();
                }

            }

            hits = 0;

        }

        void _ks_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {



            Skeleton[] skeletons = new Skeleton[0];

            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    skeletonFrame.CopySkeletonDataTo(skeletons);
                   // skeletons = (from skeletons in skeletons where skeletons.TrackingState == SkeletonTrackingState.Tracked select s).FirstOrDefault();
                }
            }


            using (DrawingContext dc = this.drawingGroup.Open())
            {

                // Draw a transparent background to set the render size
                //dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, RenderWidth, RenderHeight));

                dc.DrawRectangle(Brushes.Black, null, new Rect(0,0, Canvas.Width, Canvas.Height));

                if (skeletons.Length != 0)
                {
                    foreach (Skeleton skel in skeletons)
                    {

                        if (skel.TrackingState == SkeletonTrackingState.Tracked)
                        {
                            this.DrawBonesAndJoints(skel, dc);
                        }
                        else if (skel.TrackingState == SkeletonTrackingState.PositionOnly)
                        {
                            dc.DrawEllipse(
                            this.centerPointBrush,
                            null,
                            this.SkeletonPointToScreen(skel.Position),
                            BodyCenterThickness,
                            BodyCenterThickness);
                        }
                    }
                }
            }

        }

        void _ks_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {

        }

        private Point SkeletonPointToScreen(SkeletonPoint skelpoint)
        {
            // Convert point to depth space.  
            // We are not using depth directly, but we do want the points in our 640x480 output resolution.
            DepthImagePoint depthPoint = _ks.CoordinateMapper.MapSkeletonPointToDepthPoint(skelpoint, DepthImageFormat.Resolution640x480Fps30);
            return new Point(depthPoint.X, depthPoint.Y);
        }



        void DisKinect()
        {
            if (_ks != null)
            {
                _ks.Stop();
                _ks.AudioSource.Stop();
            }

        }

        private void Window_Loaded_1(object sender, RoutedEventArgs e)
        {
            // Create the drawing group we'll use for drawing
            this.drawingGroup = new DrawingGroup();

            // Create an image source that we can use in our image control
            this.imageSource = new DrawingImage(this.drawingGroup);

            // Display the drawing using our image control
            Image.Source = this.imageSource;

            InitializeKinect();

            LoadDefaultDirectory();

            CreateJointMappings();
        }

        private void CreateJointMappings()
        {
            //dictionary.Add(LHJoint  , JointType.HandLeft);
            //dictionary.Add(RHJoint  , JointType.HandRight);
            //dictionary.Add(RFJoint  , JointType.FootRight);
            //dictionary.Add(LFJoint  , JointType.FootLeft);
            //dictionary.Add(LAJoint  , JointType.AnkleLeft);
            //dictionary.Add(RAJoint  , JointType.AnkleRight);
            //dictionary.Add(REJoint  , JointType.ElbowRight);
            //dictionary.Add(LEJoint  , JointType.ElbowLeft);
            //dictionary.Add(HeadJoint , JointType.Head);
            //dictionary.Add(LHipJoint , JointType.HipLeft);
            //dictionary.Add(CHipJoint  , JointType.HipCenter);
            //dictionary.Add(RHipJoint  , JointType.HipRight);
            //dictionary.Add(LKJoint  , JointType.KneeLeft);
            //dictionary.Add(RKJoint  , JointType.KneeRight);
            //dictionary.Add(LSJoint  , JointType.ShoulderLeft);
            //dictionary.Add(CSJoint  , JointType.ShoulderCenter);
            //dictionary.Add(RSJoint  , JointType.ShoulderRight);
            //dictionary.Add(SpineJoint  , JointType.Spine);
            //dictionary.Add(LWJoint  , JointType.WristLeft);
            //dictionary.Add(RWJoint  , JointType.WristRight);
        }

        private void LoadDefaultDirectory()
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + "DefaultDirectory.txt";

            if (File.Exists(path))
            {
                string text = System.IO.File.ReadAllText(path);

                if (text != "")
                {
                    cboExercises.Items.Clear();

                    txtPath.Text = text;

                    DirectoryInfo dir = new DirectoryInfo(text);
                    FileInfo[] files = dir.GetFiles("*.txt");
                    foreach (FileInfo file in files)
                    {
                        cboExercises.Items.Add(file.Name.Replace(".txt", ""));
                    }
                }
            }

        }

        private void Window_Closing_1(object sender, System.ComponentModel.CancelEventArgs e)
        {
            DisKinect();
        }

        private void DrawBonesAndJoints(Skeleton skeleton, DrawingContext drawingContext)
        {
 
            float headloc = 0;

            float RighthandlocY = 0;
            float RighthandlocX = 0;
        
            int movementcount = 0;

            //during reset?
            if (ResetTime >= 20)
            {
                Reset = false;
            }


            // Render Joints
            foreach (Joint joint in skeleton.Joints)
            {
                Brush drawBrush = null;

                if (joint.TrackingState == JointTrackingState.Tracked)
                {
                    drawBrush = this.trackedJointBrush;
                }
                else if (joint.TrackingState == JointTrackingState.Inferred)
                {
                    drawBrush = this.inferredJointBrush;
                }

                if (drawBrush != null)
                {
                    drawingContext.DrawEllipse(drawBrush, null, this.SkeletonPointToScreen(joint.Position), JointThickness, JointThickness);
                }

                // find joint locations
                switch (joint.JointType)
                {
                    case JointType.HandLeft:
                        LHJoint = SkeletonPointToScreen(joint.Position);
                        break;
                    case JointType.HandRight:
                        RHJoint = SkeletonPointToScreen(joint.Position);
                        break;
                    case JointType.FootLeft:
                        LFJoint = SkeletonPointToScreen(joint.Position);
                        break;
                    case JointType.FootRight:
                        RFJoint = SkeletonPointToScreen(joint.Position);
                        break;
                    case JointType.AnkleLeft:
                        LAJoint = SkeletonPointToScreen(joint.Position);
                        break;
                    case JointType.AnkleRight:
                        RAJoint = SkeletonPointToScreen(joint.Position);
                        break;
                    case JointType.ElbowRight:
                        REJoint = SkeletonPointToScreen(joint.Position);
                        break;
                    case JointType.ElbowLeft:
                        LEJoint = SkeletonPointToScreen(joint.Position);
                        break;
                    case JointType.Head:
                        HeadJoint = SkeletonPointToScreen(joint.Position);
                        break;
                    case JointType.HipLeft:
                        LHipJoint = SkeletonPointToScreen(joint.Position);
                        break;
                    case JointType.HipCenter:
                        CHipJoint = SkeletonPointToScreen(joint.Position);
                        break;
                    case JointType.HipRight:
                        RHipJoint = SkeletonPointToScreen(joint.Position);
                        break;
                    case JointType.KneeLeft:
                        LKJoint = SkeletonPointToScreen(joint.Position);
                        break;
                    case JointType.KneeRight:
                        RKJoint = SkeletonPointToScreen(joint.Position);
                        break;
                    case JointType.ShoulderLeft:
                        LSJoint = SkeletonPointToScreen(joint.Position);
                        break;
                    case JointType.ShoulderCenter:
                        CSJoint = SkeletonPointToScreen(joint.Position);
                        break;
                    case JointType.ShoulderRight:
                        RSJoint = SkeletonPointToScreen(joint.Position);
                        break;
                    case JointType.Spine:
                        SpineJoint = SkeletonPointToScreen(joint.Position);
                        break;
                    case JointType.WristLeft:
                        LWJoint = SkeletonPointToScreen(joint.Position);
                        break;
                    case JointType.WristRight:
                        RWJoint = SkeletonPointToScreen(joint.Position);
                        break;
                    default:
                        break;
                }
            }


            //reset exercise if we have popped the last bubble
            if (PoppedBubbles.Count == TrackedPoints.Count)
            {
                PoppedBubbles.Clear();
                Reset = true;
                ResetTime = 0;
                Canvas.Children.Clear();


                //send key on completed exercise
                SendKey("z");
            }


            if (Reset == true)
            {
                ResetTime = ResetTime + 1;
            }
            else
            {

                double torsoX = Canvas.Width / 2;
                double torsoY = Canvas.Height / 2;

                Point p = new Point(torsoX, torsoY);
                //   Point _p = new Point();

                if (Motion.test == true)
                {
                    drawingContext.DrawEllipse(centerPointBrush, null, p, TorsoThickness, TorsoThickness);
                    //  txtCenter.Text = p.X.ToString() + " , " + p.Y.ToString();
                }

                if (Motion.BubbleTest == true)
                {
                    Point[] Tpoints = null;

                    //offset bubbles according to center hip position
                    // do we want to offset Y also?
                    //x
                    double Xdisplace = CHipJoint.X - p.X;

                    //need to convert to array to edit point values
                    Tpoints = TrackedPoints.ToArray();


                    for (int i = 0; i < Tpoints.Count(); i++)
                    {
                        Tpoints[i].X = Tpoints[i].X + Xdisplace;
                    }

                    List<Point> OffsetTrackedPoints = new List<Point>();

                    //save modified array to the tracked point list
                    OffsetTrackedPoints.Clear();
                    OffsetTrackedPoints = Tpoints.ToList();

                    //plot bubbles
                    for (int i = 1; i <= TrackedPoints.Count; i++)
                    {
                        if (PoppedBubbles.Count > 0)
                        {
                            if (!PoppedBubbles.Contains(i))
                            {
                                PlotNewThing(PolyType.Circle, TrackedBubbleSizes.ElementAt(i - 1), Colors.DarkBlue, OffsetTrackedPoints.ElementAt(i - 1), i, pointBrush);
                            }
                            else
                            {
                                PlotNewThing(PolyType.Circle, TrackedBubbleSizes.ElementAt(i - 1), Colors.Red, OffsetTrackedPoints.ElementAt(i - 1), i, PoppedPointBrush);
                            }
                        }
                        else
                        {
                            PlotNewThing(PolyType.Circle, TrackedBubbleSizes.ElementAt(i - 1), Colors.DarkBlue, OffsetTrackedPoints.ElementAt(i - 1), i, pointBrush);
                        }
                    }
                }

                //see if left hand is above head
                if (LefthandlocY > headloc && movementcount == 0)
                {
                    SwitchIndicatorButton(1);
                    SendKey("{LEFT}");
                    movementcount = movementcount + 1;
                }

                // see if right hand is above head

                if (RighthandlocY > headloc && movementcount == 0)
                {
                    SendKey("{RIGHT}");
                    movementcount = movementcount + 1;
                }

                // see if hands are crossed

                if (LefthandlocX > RighthandlocX && movementcount == 0)
                {
                    SendKey("z");
                    SwitchIndicatorButton(2);
                    movementcount = movementcount + 1;
                }

                Canvas.Children.Clear();
                DrawFrame(this.Canvas.Children);


                things.Clear();
            }
        }
        

        public static void SendKey(string Key)
        {
            SendKeys.SendWait(Key);
        }

        private void SwitchIndicatorButton(int Button)
        {
            if (Button == 1)
            {
                //Indicator.Background = new SolidColorBrush(Colors.Green);
            }
            else if (Button == 2)
            {
              //  CrossIndicator.Background = new SolidColorBrush(Colors.Purple);
            }
        }

        private void DrawLeftHandTest()
        {
            double torsoX = Canvas.Width / 2;
            double torsoY = Canvas.Height;

            Point p = new Point(torsoX, torsoY);

            using (DrawingContext dc = this.drawingGroup.Open())
            {
                dc.DrawEllipse(centerPointBrush, null, p, TorsoThickness, TorsoThickness);
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Motion.test = true;
        }

        private void btnPlot_Click(object sender, RoutedEventArgs e)
        {
            Motion.BubbleTest = true;

            ThingCount = 0;
     
        }

        private void PlotNewThing(PolyType newShape, double newSize, System.Windows.Media.Color newColor, Point Location, int ThingID, Brush newBrush)
        {
            bool Popped = false;
            
            sceneRect.X = Image.Width;
            sceneRect.Y = Image.Height;
            
            // Only drop within the center "square" area 
            double dropWidth = this.sceneRect.Bottom - this.sceneRect.Top;
            if (dropWidth > this.sceneRect.Right - this.sceneRect.Left)
            {
                dropWidth = this.sceneRect.Right - this.sceneRect.Left;
            }

            var newThing = new Thing
            {
                Size    = newSize,
                Popped  = Popped,
                Shape   = newShape,
                Center  = Location,
                Color   = newColor,
                Brush   = newBrush,
                ThingID = ThingID
            };

            this.things.Add(newThing);
        }

        private struct Thing
        {
            public Point Center;
            public double Size;
            public double Theta;
            public PolyType Shape;
            public System.Windows.Media.Color Color;
            public System.Windows.Media.Brush Brush;
            public System.Windows.Media.Brush Brush2;
            public System.Windows.Media.Brush BrushPulse;
            public DateTime TimeLastHit;
            public bool Popped;
            public int ThingID;
        }

        public enum PolyType
        {
            Circle = 0x40,
        }

        public void DrawFrame(UIElementCollection children)
        {
            // Draw all shapes in the scene
            for (int i = 0; i < this.things.Count; i++)
            {
                Thing thing = this.things[i];
                if (thing.Brush == null)
                {
                    thing.Brush = new SolidColorBrush(thing.Color);
                }
                
                children.Add(MakeSimpleShape(
                    thing.Size,
                    thing.Center,
                    thing.Brush,
                    thing.BrushPulse,
                    thing.Size,
                    thing.ThingID
                    ));


                //set number
                TextBlock tb = new TextBlock();
                tb.Text = thing.ThingID.ToString();
                tb.Foreground = Brushes.White;
              //  tb.Background = Brushes.White;
                tb.FontSize = 20;
                if (thing.ThingID < 10)
                {
                    tb.SetValue(Canvas.LeftProperty, thing.Center.X - 5);
                    tb.SetValue(Canvas.TopProperty, thing.Center.Y - 15);
                }
                else
                {
                    tb.SetValue(Canvas.LeftProperty, thing.Center.X - 11);
                    tb.SetValue(Canvas.TopProperty, thing.Center.Y - 15);
                }



                children.Add(tb);




                this.things[i] = thing;
                
                ThingCount = ThingCount + 1;
            }


            // do hit testing 
            List<double> distances = new List<double>();
            distances.Clear();
            
            for (int i=0;i<TrackedPoints.Count();i++)
            {
                distances.Add(Math.Sqrt(Math.Pow(TrackedPoints.ElementAt(i).X - LHJoint.X, 2) + Math.Pow(TrackedPoints.ElementAt(i).Y - LHJoint.Y, 2)));
                
              

            }

            for (int i = 0; i < distances.Count; i++)
            {
                // only do hit testing on non popped bubbles
                if (!PoppedBubbles.Contains(i + 1))
                {
                    ////only hit test in order
                    //if (PoppedBubbles.Count == 0 &&  


                    if (distances.ElementAt(i) <= TrackedBubbleSizes.ElementAt(i))
                    {
                        Thing poppedthing = things[i];

                        //do hit testing in order
                        if ((PoppedBubbles.Count == 0 && poppedthing.ThingID == 1) | 
                             (PoppedBubbles.Count > 0 && poppedthing.ThingID == PoppedBubbles.Max() + 1))
                        {
                            poppedthing.Popped = true;
                            if (poppedthing.Brush != PoppedPointBrush)
                            {
                                PoppedBubbles.Add(poppedthing.ThingID);
                            }
                        }
                    }
                }
            }
        }

        public bool PopInOrder()
        {
            bool Continue = false;


            if (PoppedBubbles.Count == 0)
            {
                Continue = false;
            }
            else
            {
                int BubbleLastPopped = PoppedBubbles.Max() + 1;
            }

            return Continue; 
        }

        private Shape MakeSimpleShape(double size, Point center, Brush brush, Brush brushStroke, double strokeThickness, int ThingID)
        {
            //set number
            //TextBlock tb = new TextBlock();
            //tb.Text = ThingID.ToString();
            //tb.Foreground = Brushes.White;
            //tb.Background = Brushes.White;
            //tb.FontSize = 20;
            //if (ThingID < 10)
            //{
            //    tb.SetValue(Canvas.LeftProperty, center.X - 5);
            //    tb.SetValue(Canvas.TopProperty, center.Y - 15);
            //}
            //else
            //{
            //    tb.SetValue(Canvas.LeftProperty, center.X - 11);
            //    tb.SetValue(Canvas.TopProperty, center.Y - 15);
            //}
            
            //create circle
            var circle = new Ellipse { Width = size * 2, Height = size * 2, Stroke = brushStroke };
           
            circle.StrokeThickness = BubbleThickness ;
            circle.Fill = brush;
            circle.SetValue(Canvas.LeftProperty, center.X - size);
            circle.SetValue(Canvas.TopProperty, center.Y - size);

           // Canvas.Children.Add(tb);
          //  circle.InputHitTest(center);
            return circle;  
        }




        public int hits { get; set; }

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            cboExercises.Items.Clear();
           
            System.Windows.Forms.FolderBrowserDialog browse = new System.Windows.Forms.FolderBrowserDialog();
            browse.ShowDialog();

            txtPath.Text = browse.SelectedPath;

            //save to app path
            
            // WriteAllLines creates a file, writes a collection of strings to the file, 
            // and then closes the file.
            System.IO.File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + "DefaultDirectory.txt", txtPath.Text);



            //populate exercise combo 
            DirectoryInfo dir = new DirectoryInfo(browse.SelectedPath);
            FileInfo[] files = dir.GetFiles("*.txt");
            foreach (FileInfo file in files)
            {
                cboExercises.Items.Add(file.Name.Replace(".txt",""));
            }

        }

        private void btnLoad_Click(object sender, RoutedEventArgs e)
        {
            if (cboExercises.Text == "")
            {
                System.Windows.Forms.MessageBox.Show("Please select an exercise.");
                return;
            }

            ExerciseArray = null;
            TrackedPoints.Clear();
            TrackedJoints.Clear();
            TrackedBubbleSizes.Clear();
            PoppedBubbles.Clear();



            
            ExerciseArray = File.ReadAllLines(txtPath.Text + "\\" + cboExercises.Text + ".txt");

            SetTrackedElements();

            //foreach (string line in ExerciseArray)
            //{

            //}
            
        }

        private void SetTrackedElements()
        {
            int NumJoints = 0; // TrackedJoints.Count();

            double BubbleSize;

            Point point = new Point();
            double _Xcoord;
            double _Ycoord;

            string _Joint;
            
            string[] splitArray;

            char[] trimChars = { '(', ')' };
         

            foreach (string line in ExerciseArray)
            {
                if (!line.Contains(cboExercises.Text))
                {
                  
                   splitArray = line.Split(';');
                   _Joint = splitArray.ElementAt(2).Trim();
                    

                   BubbleSize = Convert.ToDouble(splitArray.ElementAt(1).Trim());
                   TrackedBubbleSizes.Add(BubbleSize);

                   _Xcoord = Convert.ToDouble(splitArray.ElementAt(3).Split(',').ElementAt(0).Trim().TrimStart(trimChars));
                   _Ycoord = Convert.ToDouble(splitArray.ElementAt(3).Split(',').ElementAt(1).TrimEnd(trimChars).Trim());
                   point.X = _Xcoord;
                   point.Y = _Ycoord;

                   TrackedPoints.Add(point);

 
                   switch (_Joint)
                   {
                       case "Left Hand":
                           TrackedJoints.Add(JointType.HandLeft);
                           break;
                       case "Right Hand":
                           TrackedJoints.Add(JointType.HandRight);
                           break;
                       case "Left Foot":
                           TrackedJoints.Add(JointType.FootLeft);
                           break;
                       case "Right Foot":
                           TrackedJoints.Add(JointType.FootRight);
                           break;
                       case "Left Ankle":
                           TrackedJoints.Add(JointType.AnkleLeft);
                           break;
                       case "Right Ankle":
                           TrackedJoints.Add(JointType.AnkleRight);
                           break;
                       case "Left Elbow":
                           TrackedJoints.Add(JointType.ElbowRight);
                           break;
                       case "Right Elbow":
                           TrackedJoints.Add(JointType.ElbowRight);
                           break;
                       case "Head":
                           TrackedJoints.Add(JointType.Head);
                           break;
                       case "Left Hip":
                           TrackedJoints.Add(JointType.HipLeft);
                           break;
                       case "Center Hip":
                           TrackedJoints.Add(JointType.HipCenter);
                           break;
                       case "Right Hip":
                           TrackedJoints.Add(JointType.HipRight);
                           break;
                       case "Left Knee":
                           TrackedJoints.Add(JointType.KneeLeft);
                           break;
                       case "Right Knee":
                           TrackedJoints.Add(JointType.KneeRight);
                           break;
                       case "Left Shoulder":
                           TrackedJoints.Add(JointType.ShoulderLeft);
                           break;
                       case "Center Shoulder":
                           TrackedJoints.Add(JointType.ShoulderCenter);
                           break;
                       case "Right Shoulder":
                           TrackedJoints.Add(JointType.ShoulderRight);
                           break;
                       case "Spine":
                           TrackedJoints.Add(JointType.Spine);
                           break;
                       case "Wrist Left":
                           TrackedJoints.Add(JointType.WristLeft);
                           break;
                       case "Wrist Right":
                           TrackedJoints.Add(JointType.WristRight);
                           break;
                       default:
                           break;
                   }

                   NumJoints = NumJoints + 1;

               }
            }
        
        }


        public float LefthandlocZ { get; set; }

        public float LefthandlocX { get; set; }

        public float LefthandlocY { get; set; }

        public SkeletonPoint Point { get; set; }




        //public Point CirclePoint1 { get; set; }

        //public Point CirclePoint2 { get; set; }

        //public Point CirclePoint3 { get; set; }


        public int ThingCount { get; set; }



        



        #region Collections

        public Dictionary<Point, JointType> dictionary = new Dictionary<Point, JointType>();

        //collections for exercise information
        public string[] ExerciseArray;

        // collection of Joints for exercise
        public List<JointType> TrackedJoints = new List<JointType>();

        // collection of Points for exercise
        public List<Point> TrackedPoints = new List<Point>();
       
        // collection for bubble sizes 
        public List<double> TrackedBubbleSizes = new List<double>();

        public List<int> PoppedBubbles = new List<int>();

        public int ResetTime { get; set; }
        public bool Reset = false;

     
        // all tracked joints
        public Point LHJoint { get; set; }
        public Point RHJoint { get; set; }
        public Point RFJoint { get; set; }
        public Point LFJoint { get; set; }
        public Point LAJoint { get; set; }
        public Point RAJoint { get; set; }
        public Point REJoint { get; set; }
        public Point LEJoint { get; set; }
        public Point HeadJoint { get; set; }
        public Point LHipJoint { get; set; }
        public Point CHipJoint { get; set; }
        public Point RHipJoint { get; set; }
        public Point LKJoint { get; set; }
        public Point RKJoint { get; set; }
        public Point LSJoint { get; set; }
        public Point CSJoint { get; set; }
        public Point RSJoint { get; set; }
        public Point SpineJoint { get; set; }
        public Point LWJoint { get; set; }
        public Point RWJoint { get; set; }
        

        

        #endregion Collections
        

    }
}
