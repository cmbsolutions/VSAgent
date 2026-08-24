
'''<summary>
''' Interaction logic for VSAgentToolWindowControl.xaml
'''</summary>
Partial Public Class VSAgentToolWindowControl
    Inherits System.Windows.Controls.UserControl

    ''' <summary>
    ''' Handles click on the button by displaying a message box.
    ''' </summary>
    ''' <param name="sender">The event sender.</param>
    ''' <param name="e">The event args.</param>
    <System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1300:SpecifyMessageBoxOptions")>
    Private Sub button1_Click(ByVal sender As Object, ByVal e As System.EventArgs)

        System.Windows.MessageBox.Show(
            String.Format(System.Globalization.CultureInfo.CurrentUICulture, "Invoked {0}", Me.ToString()),
            "VSAgentToolWindow")
    End Sub
End Class