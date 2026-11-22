Imports System.Drawing
Imports System.Windows.Forms
Imports System.ComponentModel

''' <summary>
''' A Panel used as a game surface. It owns a private double buffer, so framework
''' repaints (resize, sibling controls appearing, minimize/restore) never flicker, and
''' it knows how to restore the last rendered frame instead of repainting black.
''' </summary>
Public Class DoubleBufferedPanel
    Inherits Panel

    ''' <summary>The completed frame to restore whenever the panel is repainted.</summary>
    <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
    Public Property Frame As Bitmap

    Public Sub New()
        ' OptimizedDoubleBuffer = one off-screen DC that is copied in one blit.
        ' AllPaintingInWmPaint  = background painting goes into that buffer, not the screen.
        ' ResizeRedraw          = the whole surface is invalidated on resize (once).
        SetStyle(ControlStyles.AllPaintingInWmPaint Or
                 ControlStyles.OptimizedDoubleBuffer Or
                 ControlStyles.ResizeRedraw, True)
        UpdateStyles()
        DoubleBuffered = True
        TabStop = False
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        ' Without this the panel would repaint its solid BackColor over the most recent
        ' frame whenever anything invalidates it, blanking the game between ticks.
        If Frame IsNot Nothing Then
            e.Graphics.DrawImageUnscaled(Frame, 0, 0)
            Return
        End If
        MyBase.OnPaint(e)
    End Sub

End Class
