Imports System
Imports System.Collections
Imports System.ComponentModel
Imports System.Data
Imports System.Drawing
Imports System.Runtime.InteropServices
Imports System.Windows
Imports Microsoft.VisualStudio.Shell
Imports Microsoft.VisualStudio.Shell.Interop

''' <summary>
''' This class implements the tool window exposed by this package and hosts a user control.
''' </summary>
''' <remarks>
''' In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane, 
''' usually implemented by the package implementer.
''' <para>
''' This class derives from the ToolWindowPane class provided from the MPF in order to use its 
''' implementation of the IVsUIElementPane interface.
''' </para>
''' </remarks>
<Guid("af158704-0523-4841-a76e-bfce8ca19ca9")>
Public Class VSAgentToolWindow
    Inherits ToolWindowPane

    ''' <summary>
    ''' Initializes a new instance of the <see cref="VSAgentToolWindow"/> class.
    ''' </summary>
    Public Sub New()
        MyBase.New(Nothing)
        Me.Caption = "VSAgentToolWindow"

        'This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
        'we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on 
        'the object returned by the Content property.
        Me.Content = New VSAgentToolWindowControl()
    End Sub

End Class
