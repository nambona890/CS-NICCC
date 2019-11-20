using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace CS_NICCC
{
    public partial class Form1 : Form
    {
        private byte[] byteArray = Properties.Resources.scene1;
        private Frame[] frames = new Frame[1800];
        private short frame = 0;
        private bool update = false;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            LoadData();
            System.Timers.Timer atimer = new System.Timers.Timer();
            atimer.Interval = 50;
            atimer.Elapsed += Refresh;
            atimer.AutoReset = true;
            atimer.Enabled = true;
        }

        private void Refresh(object sender, System.Timers.ElapsedEventArgs e)
        {
            update = true;
            Invalidate();
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            if (update)
            {
                DrawPolys(sender, e);
                update = false;
            }
        }

        private void LoadData()
        {
            Color[] colors = new Color[16];
            int byteIndex = 0;
            for (int f = 0; f < 1800; f++)
            {
                PointF[] vertices = new PointF[256];
                byte[][] vertexID = new byte[256][];
                byte[] polygonVerts = new byte[256];
                byte[] colorIndex = new byte[256];
                byte numOfPolys = 0;
                byte flags = byteArray[byteIndex++];
                bool clearScreen = Utils.GetBit(flags, 0);
                bool hasPalette = Utils.GetBit(flags, 1);
                bool isIndexed = Utils.GetBit(flags, 2);
                    ushort bitMask = 0;
                    if (hasPalette)
                    {
                        bitMask |= Convert.ToUInt16(byteArray[byteIndex++] << 8);
                        bitMask |= byteArray[byteIndex++];
                        for (int i = 0; i < 16; i++)
                        {
                            if (Utils.GetBit(bitMask, 15 - i))
                            {
                                ushort stC = 0;
                                stC |= Convert.ToUInt16(byteArray[byteIndex++] << 8);
                                stC |= Convert.ToUInt16(byteArray[byteIndex++]);
                                colors[15-i] = Utils.ST2RGB(stC);
                            }
                        }
                    }
                    if (isIndexed)
                    {
                        byte vertNum = byteArray[byteIndex++];
                        for (int i = 0; i < vertNum; i++)
                        {
                            PointF tempPoint = new PointF(0, 0);
                            tempPoint.X = Convert.ToSingle(byteArray[byteIndex++] / 256.0);
                            tempPoint.Y = Convert.ToSingle(byteArray[byteIndex++] / 200.0);
                            vertices[i] = tempPoint;
                        }
                        bool done = false;
                        while (!done)
                        {
                            byte bits = byteArray[byteIndex++];
                            switch (bits)
                            {
                                case 0xFF:
                                    done = true;
                                    break;
                                case 0xFE:
                                    byteIndex &= ~0xFFFF;
                                    byteIndex += 0x10000;
                                    done = true;
                                    break;
                                case 0xFD:
                                    done = true;
                                    break;
                                default:
                                    byte colInd = Convert.ToByte((bits & 0xF0) >> 4);
                                    byte polyVert = Convert.ToByte(bits & 0x0F);
                                    polygonVerts[numOfPolys] = polyVert;
                                    byte[] tempID = new byte[polyVert];
                                    for (int i = 0; i < polyVert; i++)
                                    {
                                        tempID[i] = byteArray[byteIndex++];
                                    }
                                    vertexID[numOfPolys] = tempID;
                                    colorIndex[numOfPolys++] = colInd;
                                    break;
                            }
                        }
                    }
                    else
                    {
                        byte pointInd = 0;
                        bool done = false;
                        while (!done)
                        {
                            byte bits = byteArray[byteIndex++];
                            switch (bits)
                            {
                                case 0xFF:
                                    done = true;
                                    break;
                                case 0xFE:
                                    byteIndex &= ~0xFFFF;
                                    byteIndex += 0x10000;
                                    done = true;
                                    break;
                                case 0xFD:
                                    done = true;
                                    break;
                                default:
                                    byte colInd = Convert.ToByte((bits & 0xF0) >> 4);
                                    byte polyVert = Convert.ToByte(bits & 0x0F);
                                    polygonVerts[numOfPolys] = polyVert;
                                    for (int i = 0; i < polyVert; i++)
                                    {
                                        PointF tempPoint = new PointF(0, 0);
                                        tempPoint.X = Convert.ToSingle(byteArray[byteIndex++] / 256.0);
                                        tempPoint.Y = Convert.ToSingle(byteArray[byteIndex++] / 200.0);
                                        vertices[pointInd++] = tempPoint;
                                    }
                                    colorIndex[numOfPolys++] = colInd;
                                    break;
                            }
                        }
                    }
                    Frame tmpFrame = new Frame();
                    tmpFrame.clearScreen = clearScreen;
                    tmpFrame.colorIndex = colorIndex;
                    tmpFrame.colors = colors;
                    tmpFrame.hasPalette = hasPalette;
                    tmpFrame.isIndexed = isIndexed;
                    tmpFrame.numOfPolys = numOfPolys;
                    tmpFrame.polygonVerts = polygonVerts;
                    tmpFrame.vertexID = vertexID;
                    tmpFrame.vertices = vertices;
                    frames[f] = tmpFrame;
                }
        }

        private void DrawPolys(object sender, PaintEventArgs e)
        {
            byte pointInd = 0;
            for (int i=0;i<frames[frame].numOfPolys;i++)
            {
                try
                {
                    PointF[] points = new PointF[frames[frame].polygonVerts[i]];
                    if (points.Length > 2)
                    {
                        Color polyColor = frames[frame].colors[frames[frame].colorIndex[i]];
                        Brush polyBrush = new SolidBrush(Color.Red);
                        if (polyColor != Color.Empty)
                        {
                            polyBrush = new SolidBrush(polyColor);
                        }
                        if (!frames[frame].isIndexed)
                        {
                            for (int j = 0; j < points.Length; j++)
                            {
                                points[j] = frames[frame].vertices[pointInd++];
                            }
                        }
                        else
                        {
                            for (int j = 0; j < points.Length; j++)
                            {
                                points[j] = frames[frame].vertices[frames[frame].vertexID[i][j]];
                            }
                        }
                        for (int j = 0; j < points.Length; j++)
                        {
                            points[j].X *= Width;
                            points[j].Y *= Height;
                        }
                        e.Graphics.FillPolygon(polyBrush, points);
                    }
                }
                catch
                {

                }
            }
            frame++;
            if (frame>=1800)
            {
                frame = 0;
            }
        }

        private void Form1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if(e.KeyChar == 'r' || e.KeyChar == 'R')
            {
                frame = 0;
            }
        }
    }

    public class Frame
    {
        public PointF[] vertices = new PointF[256];
        public Color[] colors = new Color[16];
        public byte[][] vertexID = new byte[256][];
        public byte[] colorIndex = new byte[256];
        public byte[] polygonVerts = new byte[256];
        public byte numOfPolys = 0;
        public bool clearScreen, hasPalette, isIndexed;

        public Frame()
        {

        }
    }

    public static class Utils
    {
        public static Color ST2RGB(ushort stC)
        {
            try
            {
                byte blue = Convert.ToByte(((stC & 0x007) & 0xff) << 5);
                byte green = Convert.ToByte((((stC & 0x070) >> 4) & 0xff) << 5);
                byte red = Convert.ToByte((((stC & 0x700) >> 8) & 0xff) << 5);

                return Color.FromArgb(red, green, blue);
            }
            catch
            {
                return Color.Black;
            }
        }

        public static bool GetBit(this byte b, int bitNumber)
        {
            return (b & (1 << bitNumber)) != 0;
        }

        public static bool GetBit(this ushort b, int bitNumber)
        {
            return (b & (1 << bitNumber)) != 0;
        }
    }
}
