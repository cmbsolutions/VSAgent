Imports System.Drawing
Imports System.Drawing.Drawing2D

''' <summary>
''' Renders all game entities onto a Graphics surface. Pure rendering — no game logic.
''' All draw calls are minimal and use pre-loaded sprite images when available,
''' with fast fallback shapes otherwise.
''' </summary>
Public Class SpaceShooterRenderer

    ''' <summary>Draw the entire game frame.</summary>
    Public Shared Sub Render(g As Graphics, engine As GameEngine)
        g.SmoothingMode = SmoothingMode.AntiAlias
        g.InterpolationMode = InterpolationMode.NearestNeighbor   ' pixel-perfect sprites

        ' Clear to deep space blue-black
        g.Clear(Color.FromArgb(0, 0, 10))

        ' Apply screen shake offset when active
        If engine.ShakeOffsetX <> 0 OrElse engine.ShakeOffsetY <> 0 Then
            g.TranslateTransform(CSng(engine.ShakeOffsetX), CSng(engine.ShakeOffsetY))
        End If

        RenderStarfield(g, engine)
        RenderBullets(g, engine)
        RenderEnemies(g, engine)
        RenderPlayer(g, engine)
        RenderExplosions(g, engine)
        RenderHUD(g, engine)
        RenderGameOver(g, engine)

        ' Restore transform after shake
        If engine.ShakeOffsetX <> 0 OrElse engine.ShakeOffsetY <> 0 Then
            g.ResetTransform()
        End If
    End Sub

    ''' <summary>Draw the scrolling starfield by tiling StarsImage across the canvas with scroll offset.</summary>
    Private Shared Sub RenderStarfield(g As Graphics, engine As GameEngine)
        Dim tileW As Integer = 100   ' default tile size for stars.png
        Dim tileH As Integer = 75
        If engine.StarsImage IsNot Nothing AndAlso engine.StarsImage.Width > 0 Then
            tileW = engine.StarsImage.Width
            tileH = engine.StarsImage.Height

            ' Draw the starfield image tiled across the entire canvas with scroll offsets
            Dim scrollY As Double = engine.ScrollOffsetYValue Mod tileH
            ' Count how many tiles wide/tall we need to cover the panel
            Dim colsNeeded As Integer = (engine.SurfaceWidth \ tileW) + 2
            Dim rowsNeeded As Integer = (engine.SurfaceHeight \ tileH) + 2
            For row As Integer = -1 To rowsNeeded
                For col As Integer = -1 To colsNeeded
                    g.DrawImage(engine.StarsImage,
                                CSng(col * tileW),
                                CSng(row * tileH - scrollY),
                                CSng(tileW),
                                CSng(tileH))
                Next
            Next
        Else
            ' Fallback: draw scattered dots as stars
            Dim rng As New Random(42)
            For i As Integer = 0 To 150 - 1
                Dim sx As Double = rng.Next(0, engine.SurfaceWidth)
                Dim sy As Double = (rng.NextDouble() * engine.SurfaceHeight + engine.ScrollOffsetYValue * 0.3) Mod engine.SurfaceHeight
                If sy < 0 Then sy += engine.SurfaceHeight
                Dim sz As Integer = rng.Next(1, 3)
                Using br As New SolidBrush(Color.FromArgb(CByte(rng.Next(80, 200)), Color.White))
                    g.FillEllipse(br, CSng(sx), CSng(sy), CSng(sz), CSng(sz))
                End Using
            Next
        End If
    End Sub

    ''' <summary>Draw all bullets.</summary>
    Private Shared Sub RenderBullets(g As Graphics, engine As GameEngine)
        For Each b In engine.Bullets
            If engine.BulletImage IsNot Nothing AndAlso engine.BulletImage.Width > 0 Then
                g.DrawImage(engine.BulletImage, CSng(b.X), CSng(b.Y), CSng(b.Width), CSng(b.Height))
            Else
                ' Fallback: bright blue beam with glow
                Using glowBrush As New SolidBrush(Color.FromArgb(60, 100, 200, 255))
                    g.FillEllipse(glowBrush, CSng(b.X - b.Width), CSng(b.Y - 4), CSng(b.Width * 3), CSng(b.Height + 8))
                End Using
                Using br As New SolidBrush(Color.FromArgb(128, 140, 255))
                    g.FillRectangle(br, CSng(b.X), CSng(b.Y), CSng(b.Width), CSng(b.Height))
                End Using
            End If
        Next
    End Sub

    ''' <summary>Draw all enemies.</summary>
    Private Shared Sub RenderEnemies(g As Graphics, engine As GameEngine)
        For Each ene In engine.Enemies
            If engine.EnemyImage IsNot Nothing AndAlso engine.EnemyImage.Width > 0 Then
                g.DrawImage(engine.EnemyImage, CSng(ene.X), CSng(ene.Y), CSng(ene.Width), CSng(ene.Height))
            Else
                ' Fallback: red triangle
                Using pen As New Pen(Color.Red, 2.0F)
                    Dim pts() As PointF = {
                        New PointF(CSng(ene.X + ene.Width / 2), CSng(ene.Y + ene.Height)),
                        New PointF(CSng(ene.X), CSng(ene.Y)),
                        New PointF(CSng(ene.X + ene.Width), CSng(ene.Y))
                    }
                    g.FillPolygon(Brushes.DarkRed, pts)
                    g.DrawPolygon(pen, pts)
                End Using
            End If
        Next
    End Sub

    ''' <summary>Draw the player ship.</summary>
    Private Shared Sub RenderPlayer(g As Graphics, engine As GameEngine)
        If Not engine.PlayerAlive Then Return

        Dim px = engine.PlayerX
        Dim py = engine.PlayerY
        Dim pw = GameEngine.PLAYER_WIDTH
        Dim ph = GameEngine.PLAYER_HEIGHT

        If engine.PlayerImage IsNot Nothing AndAlso engine.PlayerImage.Width > 0 Then
            g.DrawImage(engine.PlayerImage, CSng(px), CSng(py), CSng(pw), CSng(ph))

            ' Engine flame (animated)
            Dim t As Double = Environment.TickCount64 / 50.0
            Dim flameH As Single = CSng(8 + Math.Sin(t) * 3)
            Using flameBrush As New SolidBrush(Color.FromArgb(255, 255, CByte(Math.Max(0, 150 + CInt(Math.Sin(t * 0.8) * 105))), 0))
                g.FillRectangle(flameBrush, CSng(px + pw * 0.3), CSng(py + ph), CSng(pw * 0.4), flameH)
            End Using
        Else
            ' Fallback: draw a simple triangle shape
            Using pen As New Pen(Color.FromArgb(0, 200, 255), 2.0F)
                Dim pts() As PointF = {
                    New PointF(CSng(px + pw / 2), CSng(py)),
                    New PointF(CSng(px), CSng(py + ph)),
                    New PointF(CSng(px + pw), CSng(py + ph))
                }
                g.FillPolygon(Brushes.BlueViolet, pts)
                g.DrawPolygon(pen, pts)

                ' Engine flame
                Using flameBrush As New SolidBrush(Color.FromArgb(255, 255, 150, 0))
                    g.FillRectangle(flameBrush, CSng(px + pw * 0.3), CSng(py + ph), CSng(pw * 0.4), 8)
                End Using
            End Using
        End If
    End Sub

    ''' <summary>Draw explosion particles.</summary>
    Private Shared Sub RenderExplosions(g As Graphics, engine As GameEngine)
        For Each p In engine.Explosions
            Dim progress As Double = 1.0 - CSng(p.Life) / (24.0 / 60.0) ' 0 = fresh, 1 = dead
            Dim alpha As Byte = CByte(Math.Max(0, Math.Min(255, p.Life * 60)))
            If alpha <= 0 Then Continue For

            Dim size As Single = CSng(progress * 30 + 2)
            ' Outer glow (red-orange)
            Using br As New SolidBrush(Color.FromArgb(alpha, 255, CByte(Math.Max(0, 150 - p.Life * 6)), 0))
                g.FillEllipse(br, CSng(p.X - size), CSng(p.Y - size), size * 2, size * 2)
            End Using
            ' Inner bright core (yellow-white)
            Dim innerSize As Single = size * 0.45F
            If innerSize > 1 Then
                Using br As New SolidBrush(Color.FromArgb(CByte(Math.Max(0, alpha - 30)), 255, 255, CByte(Math.Min(255, 200 + p.Life * 4))))
                    g.FillEllipse(br, CSng(p.X - innerSize), CSng(p.Y - innerSize), innerSize * 2, innerSize * 2)
                End Using
            End If
        Next
    End Sub

    ''' <summary>Draw HUD overlay.</summary>
    Private Shared Sub RenderHUD(g As Graphics, engine As GameEngine)
        Dim hudFontBig As New Font("Consolas", 14.0F, FontStyle.Bold)
        Using scoreBrush As New SolidBrush(Color.White)
            g.DrawString($"Score: {engine.Score}", hudFontBig, scoreBrush, 8, 8)
            g.DrawString($"Lives: {engine.Lives}", hudFontBig, scoreBrush, CSng(engine.SurfaceWidth - CInt(g.MeasureString($"Lives: {engine.Lives}", hudFontBig).Width)) - 8, 8)
        End Using
    End Sub

    ''' <summary>Draw game over overlay.</summary>
    Private Shared Sub RenderGameOver(g As Graphics, engine As GameEngine)
        If Not engine.GameOver Then Return

        ' Darken overlay
        Using br As New SolidBrush(Color.FromArgb(100, Color.Black))
            g.FillRectangle(br, 0, 0, CSng(engine.SurfaceWidth), CSng(engine.SurfaceHeight))
        End Using

        Dim goFont As New Font("Segoe UI", 36.0F, FontStyle.Bold)
        Using br As New SolidBrush(Color.FromArgb(180, Color.Red))
            g.DrawString("GAME OVER", goFont, br,
                CSng((engine.SurfaceWidth - CInt(g.MeasureString("GAME OVER", goFont).Width)) / 2),
                CSng(engine.SurfaceHeight / 2 - 30))
        End Using

        Dim subFont As New Font("Segoe UI", 14.0F)
        Using br As New SolidBrush(Color.FromArgb(150, Color.White))
            g.DrawString($"Final Score: {engine.Score}", subFont, br,
                CSng((engine.SurfaceWidth - CInt(g.MeasureString($"Final Score: {engine.Score}", subFont).Width)) / 2),
                CSng(engine.SurfaceHeight / 2 + 20))
        End Using

        goFont.Dispose()
        subFont.Dispose()
    End Sub

End Class
