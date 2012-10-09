Imports System.IO
Public Class Viewer
    Dim filename As String
    Private Sub btnOpen_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnOpen.Click
        Dim oReader As StreamReader
        OpenFileDialog1.CheckFileExists = True
        OpenFileDialog1.CheckPathExists = True
        OpenFileDialog1.DefaultExt = "txt"
        OpenFileDialog1.FileName = ""
        OpenFileDialog1.Filter = "Log Files (*.log)|*.log|All Files (*.*)|*.*"
        OpenFileDialog1.Multiselect = False
        Try
            If OpenFileDialog1.ShowDialog = Windows.Forms.DialogResult.OK Then
                oReader = New StreamReader(OpenFileDialog1.FileName, True)
                filename = OpenFileDialog1.FileName
                txtLog.Clear()
                txtLog.Text = oReader.ReadToEnd
                Me.Text = "Log Viewer - Open file: " & filename
                oReader.Close()
            End If
        Catch ex As Exception
            txtLog.Text = "Error opening file. Please try another"
        End Try
    End Sub
End Class