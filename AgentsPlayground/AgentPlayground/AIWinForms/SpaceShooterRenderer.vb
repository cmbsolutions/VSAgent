Imports System.Drawing
Imports System.Drawing.Drawing2D

''' <summary>
''' Renders all game entities onto a Graphics surface. Pure rendering — no game logic.
''' </summary>
Public Class SpaceShooterRenderer

    ''' <summary>Draw the entire game frame.</summary>
    Public Shared Sub Render(g As Graphics, engine As GameEngine)
        g.SmoothingMode = SmoothingMode.AntiAlias
        g.InterpolationMode = InterpolationMode.HighQualityBilinear

        ' Clear to deep space blue-black
        g.Clear(Color.FromArgb(0, 0, 10))

        RenderStarfield(g, engine)
        RenderBullets(g, engine)
        RenderEnemies(g, engine)
        RenderPlayer(g, engine)
        RenderExplosions(g, engine)
        RenderHUD(g, engine)
        RenderGameOver(g, engine)
    End Sub

    ''' <summary>Draw the scrolling starfield.</summary>
    Private Shared Sub RenderStarfield(g As Graphics, engine As GameEngine)
        If engine.StarImage IsNot Nothing AndAlso engine.StarImage.Width > 0 Then
            ' Draw pre-rendered star image tiles
            Dim scrollY As Double = engine.ScrollOffsetYValue Mod 3000.0
            For Each star In engine.Stars
                Dim drawY As Double = (star.Y - scrollY)
                ' Wrap stars that go off the top back to bottom
                While drawY < 0 : drawY += 3000.0 : End While
                If drawY > engine.SurfaceHeight + star.Size Then Continue For

                g.DrawImage(engine.StarImage,
                            CSng(star.X),
                            CSng(drawY - star.Size / 2.0),
                            CSng(CSng(star.Size) * 1.5F),
                            CSng(CSng(star.Size) * 1.5F))
            Next
        Else
            ' Fallback: draw stars as white dots
            Dim scrollY As Double = engine.ScrollOffsetYValue Mod 3000.0
            For Each star In engine.Stars
                Dim drawY As Double = (star.Y - scrollY)
                While drawY < 0 : drawY += 3000.0 : End While
                If drawY > engine.SurfaceHeight + star.Size Then Continue For

                Dim alpha As Integer = CInt(150 + star.Size * 40)
                Using br As New SolidBrush(Color.FromArgb(alpha, Color.White))
                    g.FillEllipse(br, CSng(star.X), CSng(drawY - star.Size / 2.0),
                                  CSng(star.Size), CSng(star.Size))
                End Using
            Next

            ' Add extra scattered tiny stars for depth (static background layer)
            Dim rng As New Random(42)
            For i As Integer = 0 To 100 - 1
                Dim sx As Double = rng.Next(0, engine.SurfaceWidth)
                Dim sy As Double = (rng.NextDouble() * engine.SurfaceHeight + engine.ScrollOffsetYValue * 0.3) Mod engine.SurfaceHeight
                If sy < 0 Then sy += engine.SurfaceHeight
                Using br As New SolidBrush(Color.FromArgb(80, Color.White))
                    g.FillEllipse(br, CSng(sx), CSng(sy), 1.0F, 1.0F)
                End Using
            Next
        End If
    End Sub

    ''' <summary>Draw all bullets.</summary>
    Private Shared Sub RenderBullets(g As Graphics, engine As GameEngine)
        For Each b In engine.Bullets
            If engine.StarImage IsNot Nothing AndAlso engine.StarImage.Width > 0 Then
                g.DrawImage(engine.StarImage, CSng(b.X), CSng(b.Y), CSng(b.Width), CSng(b.Height))
            Else
                Using br As New SolidBrush(Color.FromArgb(128, 140, 255))
                    ' Add glow effect
                    Using glowBrush As New SolidBrush(Color.FromArgb(60, 100, 200, 255))
                        g.FillEllipse(glowBrush, CSng(b.X - b.Width), CSng(b.Y - 4), CSng(b.Width * 3), CSng(b.Height + 8))
                    End Using
                    g.FillRectangle(br, CSng(b.X), CSng(b.Y), CSng(b.Width), CSng(b.Height))
                End Using
            End If
        Next
    End Sub

    ''' <summary>Draw all enemies.</summary>
    Private Shared Sub RenderEnemies(g As Graphics, engine As GameEngine)
        For Each ene In engine.Enemies
            If engine.StarImage IsNot Nothing AndAlso engine.StarImage.Width > 0 Then
                g.DrawImage(engine.StarImage, CSng(ene.X), CSng(ene.Y), CSng(ene.Width), CSng(ene.Height))
            Else
                ' Fallback: draw enemy as red triangle
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

        If engine.StarImage IsNot Nothing AndAlso engine.StarImage.Width > 0 Then
            g.DrawImage(engine.StarImage,
                        CSng(engine.PlayerX),
                        CSng(engine.PlayerY),
                        CSng(CSng(engine.StarImage.Width) * (PLAYER_WIDTH / CSng(engine.StarImage.Width))),
                        CSng(CSng(engine.StarImage.Height) * (PLAYER_HEIGHT / CSng(engine.StarImage.Height))))

            ' Engine flame
            Dim flameH As Single = 8 + CSng(Math.Sin(Environment.TickCount64 / 50.0)) * 3
            Using flameBrush As New SolidBrush(Color.FromArgb(255, 200, CByte(Math.Max(0, 150 + CInt(Math.Sin(Environment.TickCount64 / 80.0) * 105))), 0))
                g.FillRectangle(flameBrush,
                    CSng(engine.PlayerX + PLAYER_WIDTH * 0.3),
                    CSng(engine.PlayerY + PLAYER_HEIGHT),
                    CSng(PLAYER_WIDTH * 0.4),
                    flameH)
            End Using
        Else
            ' Fallback: draw a simple triangle shape
            Using pen As New Pen(Color.FromArgb(0, 200, 255), 2.0F)
                Dim pts() As PointF = {
                    New PointF(CSng(engine.PlayerX + PLAYER_WIDTH / 2), CSng(engine.PlayerY)),
                    New PointF(CSng(engine.PlayerX), CSng(engine.PlayerY + PLAYER_HEIGHT)),
                    New PointF(CSng(engine.PlayerX + PLAYER_WIDTH), CSng(engine.PlayerY + PLAYER_HEIGHT))
                }
                g.FillPolygon(Brushes.BlueViolet, pts)
                g.DrawPolygon(pen, pts)

                ' Engine flame
                Using flameBrush As New SolidBrush(Color.FromArgb(255, 255, 150, 0))
                    g.FillRectangle(flameBrush,
                        CSng(engine.PlayerX + PLAYER_WIDTH * 0.3),
                        CSng(engine.PlayerY + PLAYER_HEIGHT),
                        CSng(PLAYER_WIDTH * 0.4),
                        8)
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
        Dim hudFont As New Font("Consolas", 9.0F)
        Using hBrush As New SolidBrush(Color.FromArgb(100, Color.White))
            g.DrawString($"Wave: {engine.EnemyWaveCount}", hudFont, hBrush, 8, CSng(engine.SurfaceHeight - 20))
        End Using

        ' Score and lives at top
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
                CSNG(engine.SurfaceHeight / 2 + 20))
        End Using

        goFont.Dispose()
        subFont.Dispose()
    End Sub

    ' Need to import PLAYER_WIDTH and PLAYER_HEIGHT
    Private Const PLAYER_WIDTH As Integer = GameEngine.PLAYER_WIDTH
    Private Const PLAYER_HEIGHT As Integer = GameEngine.PLAYER_HEIGHT
End Class
