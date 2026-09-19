<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class SpaceShooterForm
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        components = New ComponentModel.Container()
        gamePanel = New Panel()
        scoreLabel = New Label()
        statusText = New Label()
        restartButton = New Button()
        gameTimer = New Timer(components)
        SuspendLayout()
        ' 
        ' gamePanel
        ' 
        gamePanel.BackColor = Color.Black
        gamePanel.BorderStyle = BorderStyle.FixedSingle
        gamePanel.Location = New Point(12, 46)
        gamePanel.Margin = New Padding(4, 3, 4, 3)
        gamePanel.Name = "gamePanel"
        gamePanel.Size = New Size(900, 700)
        gamePanel.TabIndex = 4
        ' 
        ' scoreLabel
        ' 
        scoreLabel.AutoSize = True
        scoreLabel.Font = New Font("Segoe UI", 12F, FontStyle.Bold)
        scoreLabel.ForeColor = Color.White
        scoreLabel.Location = New Point(12, 10)
        scoreLabel.Margin = New Padding(4, 0, 4, 0)
        scoreLabel.Name = "scoreLabel"
        scoreLabel.Size = New Size(69, 21)
        scoreLabel.TabIndex = 3
        scoreLabel.Text = "Score: 0"
        ' 
        ' statusText
        ' 
        statusText.AutoSize = True
        statusText.Font = New Font("Segoe UI", 10F, FontStyle.Italic)
        statusText.ForeColor = Color.LightGray
        statusText.Location = New Point(550, 10)
        statusText.Margin = New Padding(4, 0, 4, 0)
        statusText.Name = "statusText"
        statusText.Size = New Size(253, 19)
        statusText.TabIndex = 2
        statusText.Text = "Arrow Keys/WASD to Move — Space to Shoot"
        ' 
        ' restartButton
        ' 
        restartButton.Location = New Point(780, 754)
        restartButton.Margin = New Padding(4, 3, 4, 3)
        restartButton.Name = "restartButton"
        restartButton.Size = New Size(120, 35)
        restartButton.TabIndex = 1
        restartButton.Text = "Restart"
        restartButton.UseVisualStyleBackColor = True
        restartButton.Visible = False
        ' 
        ' gameTimer
        ' 
        gameTimer.Interval = 16
        ' 
        ' SpaceShooterForm
        ' 
        AutoScaleDimensions = New SizeF(7F, 15F)
        AutoScaleMode = AutoScaleMode.Font
        BackColor = Color.Black
        ClientSize = New Size(928, 802)
        Controls.Add(restartButton)
        Controls.Add(statusText)
        Controls.Add(scoreLabel)
        Controls.Add(gamePanel)
        DoubleBuffered = True
        FormBorderStyle = FormBorderStyle.FixedSingle
        KeyPreview = True
        Margin = New Padding(4, 3, 4, 3)
        MaximizeBox = False
        MinimizeBox = False
        Name = "SpaceShooterForm"
        StartPosition = FormStartPosition.CenterScreen
        Text = "Space Shooter"
        ResumeLayout(False)
        PerformLayout()

    End Sub

    Friend WithEvents gamePanel As Panel
    Friend WithEvents scoreLabel As Label
    Friend WithEvents statusText As Label
    Friend WithEvents restartButton As Button
    Friend WithEvents gameTimer As Timer

End Class