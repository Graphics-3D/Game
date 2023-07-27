using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Engine;
using Engine.Core;
using Engine.Meshes;

public class Screen : Form
{
    private bool isRunning = true;
    private Camera cam = null!;
    private Graphics g = null!;
    private PictureBox pb = new()
    {
        Dock = DockStyle.Fill
    };

    public Screen()
    {
        renderTimes.Enqueue(DateTime.Now);
        
        Cursor.Hide();

        this.WindowState = FormWindowState.Maximized;
        this.FormBorderStyle = FormBorderStyle.None;
        this.Controls.Add(pb);

        this.Load += delegate
        {
            cam = new Camera(new Point3D(-200, 0, 0), new Vector3(1, 0, 0), new Vector3(0, 1, 0), pb.Width, pb.Height, 1000f, 1000);
            var bmp = new Bitmap(pb.Width, pb.Height);
            g = Graphics.FromImage(bmp);
            pb.Image = bmp;

            CenterScreen = new Point(pb.Width / 2, pb.Height / 2);
        };

        this.KeyUp += KeyBindUp;
        this.KeyDown += KeyBindDown;
        this.pb.MouseMove += MouseControl;
    }

    [DllImport("user32.dll", EntryPoint = "SetCursorPos")]
    private static extern bool SetCursorPos(int X, int Y);

    private Point CenterScreen;
    private void MouseControl(object? o, MouseEventArgs e)
    {
        var sensibility = 1000f;
        var x = -(CenterScreen.X - e.X) / sensibility;
        var y = -(CenterScreen.Y - e.Y) / sensibility;
        
        var sinZ = MathF.Sin(x);
        var cosZ = MathF.Cos(x);

        var sinY = MathF.Sin(y);
        var cosY = MathF.Cos(y);
        
        // cam?.Rotate(cosY, sinY, cosZ, sinZ);

        cam?.RotateZ(cosZ, sinZ);
        cam?.RotateY(cosY, sinY);
        
        SetCursorPos(CenterScreen.X, CenterScreen.Y);
    }

    private float xVel = 0;
    private float yVel = 0;
    private float zVel = 0;
    private float velocity = 2f;
    private float wVelX = 0;
    private float sVelX = 0;
    private float aVelX = 0;
    private float dVelX = 0;
    private float wVelY = 0;
    private float sVelY = 0;
    private float aVelY = 0;
    private float dVelY = 0;
    
    private void KeyBindDown(object? o, KeyEventArgs e)
    {
        var key = e.KeyCode;

        if (key == Keys.Escape)
        {
            isRunning = false;
            Application.Exit();
        }

        if (key == Keys.W)
        {
            wVelX = cam.Normal.X * velocity; 
            xVel += wVelX;

            wVelY = cam.Normal.Y * velocity; 
            yVel += wVelY;
        }

        else if (key == Keys.S)
        {
            sVelX = cam.Normal.X * velocity; 
            xVel -= sVelX;

            sVelY = cam.Normal.Y * velocity; 
            yVel -= sVelY;
        }

        if (key == Keys.A)
        {
            xVel -= cam.Horizontal.X * velocity;
            yVel -= cam.Horizontal.Y * velocity;
        }

        else if (key == Keys.D)
        {
            xVel += cam.Horizontal.X * velocity;
            yVel += cam.Horizontal.Y * velocity;
        }
        
        if (key == Keys.Space)
            Jump();
    }

    private void KeyBindUp(object? o, KeyEventArgs e)
    {
        var key = e.KeyCode;

        if (key == Keys.W)
        {
            xVel -= wVelX;
            yVel -= wVelY;
        }

        if (key == Keys.S)
        {
            xVel += sVelX;
            yVel += sVelY;
        }

        if (key == Keys.A)
        {
            xVel += cam.Horizontal.X * velocity;
            yVel += cam.Horizontal.Y * velocity;
        }
        
        if (key == Keys.D)
        {
            xVel -= cam.Horizontal.X * velocity;
            yVel -= cam.Horizontal.Y * velocity;
        }
    }

    private void checkMovement()
    {
        cam.Translate(xVel, yVel, zVel);

        foreach (var mesh in Scene.Current.Meshes)
        {
            if (mesh is Cube cube)
            {
                if (cube.Collided(cam.Location) == CollidedResult.True)
                {
                    cam.Translate(-xVel, -yVel, -zVel);
                    break;
                }
            }
        }
    }

    public void Run()
    {
        while (isRunning)
        {
            getFPS();
            updateJump();
            checkMovement();

            cam?.Render();
            cam?.Draw(g);

            g.DrawString($"{fps} fps", DefaultFont, Brushes.Red, new PointF(50.0F, 50.0F));
            g.DrawString($"{cam?.Normal}", DefaultFont, Brushes.Black, new PointF(50.0F, 100.0F));

            var cube = Scene.Current.Meshes[0] as Cube;
            if (cam?.Location is not null)
                g.DrawString($"Collided: {cube?.Collided(cam.Location)}", DefaultFont, Brushes.Black, new PointF(50.0F, 150.0F));



            pb?.Refresh();
            Application.DoEvents();
        }
    }

    #region FPS

    private int fps = 0;
    private int precisionFPS = 19;
    private Queue<DateTime> renderTimes = new();
    void getFPS()
    {
        var now = DateTime.Now;
        renderTimes.Enqueue(now);

        if (renderTimes.Count > precisionFPS)
        {
            DateTime old = renderTimes.Dequeue();
            var time = now - old;
            fps = (int)(precisionFPS / time.TotalSeconds);
        }
    }

    #endregion

    #region Jump

    private bool isJumping = false;
    void Jump()
    {
        if (isJumping)
            return;

        zVel = 10;
        isJumping = true;
    }

    void updateJump()
    {
        if (!isJumping)
            return;
        
        zVel -= 1f;

        if (cam?.Location.Z < 0)
        {
            cam.Location = cam.Location with
            {
                Z = 0
            };
            
            zVel = 0;
            isJumping = false;
        }
    }

    #endregion
}