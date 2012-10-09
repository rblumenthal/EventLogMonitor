Imports System.IO
Imports System.Net.Mail
Imports System.Threading
Imports System.Diagnostics

Public Class Form1
    Dim appdir As String = Application.StartupPath() & "\"
    Dim timespan As DateTime '= DateTime.Today.AddDays(-3)
    Dim smtp As New SmtpClient '("192.168.208.13")
    Dim mail As New MailMessage()
    Dim servers As New List(Of String)()
    Dim trd As Thread
    Dim errWriter As New StreamWriter(appdir & "errors.log", True)
    Dim fromempty As Boolean
    Dim toempty As Boolean
    Dim intervalempty As Boolean
    Dim smtpempty As Boolean
    Dim radMinUnchecked As Boolean
    Dim radDayUnchecked As Boolean
    Dim emptyDaysToScan As Boolean
    Dim sSource As String = "Security Log Monitor"
    Dim sLog As String = "Application"
    Dim sEvent As String
    Dim sMachine As String = "."
    Dim running As Boolean
    Dim timerenabled As Boolean

    Sub getSecurity(ByVal servername As String)
        Try
            Dim elEvent As New System.Diagnostics.EventLog("Security", servername)
            Dim elEventEntry As System.Diagnostics.EventLogEntry
            ListView1.Items.Clear()
            For Each elEventEntry In elEvent.Entries
                Dim li As New ListViewItem()
                With li
                    If elEventEntry.EventID = "662" Or elEventEntry.EventID = "630" Or elEventEntry.EventID = "633" Or elEventEntry.EventID = "660" Or elEventEntry.EventID = "661" Or elEventEntry.EventID = "634" Then
                        If elEventEntry.TimeGenerated > timespan Then
                            .Text = elEventEntry.EntryType.ToString
                            .SubItems.Add(elEventEntry.TimeGenerated.ToString)
                            .SubItems.Add(elEventEntry.Source.ToString)
                            .SubItems.Add(elEventEntry.EventID.ToString)

                            If elEventEntry.UserName Is Nothing Then
                                .SubItems.Add("N/A")
                            Else
                                .SubItems.Add(elEventEntry.UserName.ToString)
                            End If
                            .SubItems.Add(elEventEntry.MachineName.ToString)
                            .SubItems.Add(elEventEntry.Message.ToString)
                            ListView1.Items.Add(li)
                            li = Nothing
                        End If
                    End If
                End With
            Next
            saveInfo(servername)
        Catch ex As Exception
            errWriter.WriteLine(ex.ToString)
            Dim ELog As New EventLog(sLog, sMachine, sSource)
            ELog.WriteEntry(ex.ToString, EventLogEntryType.Error, 234, CType(3, Short))
            ToolStripStatusLabel2.Text = "Error detected. Please see Event Viewer for more details."
        End Try
    End Sub

    Private Sub Form1_FormClosing(ByVal sender As Object, ByVal e As System.Windows.Forms.FormClosingEventArgs) Handles Me.FormClosing
        If timerenabled = True Then
            If MessageBox.Show("A timer is still enabled and currently running. Are you sure you want to close?", "Confirm", MessageBoxButtons.YesNo) = Windows.Forms.DialogResult.Yes Then
                Me.Close()
            Else
                e.Cancel = True
            End If
        End If
    End Sub
    Private Sub Form1_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        DateTimePicker1.CustomFormat = "MMMM dddd yyyy HH:mm tt"
        With ListView1 ' ListView1 is not seen on the form. Used just to save information from.
            .Clear()
            .Columns.Add("Entry Type", 85, HorizontalAlignment.Left)
            .Columns.Add("Date/Time Generated", 140, HorizontalAlignment.Left)
            .Columns.Add("Source", 150, HorizontalAlignment.Left)
            .Columns.Add("Event", 50, HorizontalAlignment.Left)
            .Columns.Add("User", 150, HorizontalAlignment.Left)
            .Columns.Add("Computer", 150, HorizontalAlignment.Left)
            .Columns.Add("Description", 150, HorizontalAlignment.Left)
            .View = View.Details
        End With
        ToolStripStatusLabel1.Text = "Not Running"
        ToolStripStatusLabel2.Text = "Awaiting results"
        ToolStripStatusLabel5.Text = "Next scan not scheduled"

        DateTimePicker1.Enabled = False
        txtFromEmail.Enabled = False
        txtToEmail.Enabled = False
        txtServers.Enabled = False
        txtInterval.Enabled = False
        txtSMTP.Enabled = False
        radMinutes.Enabled = False
        radDays.Enabled = False
        txtDaysToScan.Enabled = False

        For Each Entry As String In My.Settings.Servers
            txtServers.Text &= Entry & Environment.NewLine
        Next

        txtFromEmail.Text = My.Settings.FromEmail
        txtToEmail.Text = My.Settings.ToEmail
        txtInterval.Text = My.Settings.Interval
        txtSMTP.Text = My.Settings.SMTPServer
        txtDaysToScan.Text = My.Settings.DaysToScan

        If My.Settings.IntervalLength = "Days" Then
            radDays.Checked = True
        ElseIf My.Settings.IntervalLength = "Minutes" Then
            radMinutes.Checked = True
        End If
    End Sub
    Sub saveInfo(ByVal servername As String)
        Try
            Dim Writer As New StreamWriter(appdir & servername & "_" & timespan.ToString("MM-dd-yyyy") & "-" & Date.Today.ToString("MM-dd-yyyy") & ".log")
            For i As Integer = 0 To Me.ListView1.Items.Count - 1
                For j As Integer = 0 To Me.ListView1.Columns.Count - 1
                    Writer.Write(Me.ListView1.Items(i).SubItems(j).Text & ControlChars.Tab)
                Next
                Writer.WriteLine()
            Next
            Writer.Close()
            ToolStripStatusLabel2.Text = ""
        Catch ex As Exception
            errWriter.WriteLine(Date.Now)
            errWriter.WriteLine(ex.ToString)
            errWriter.WriteLine("")
            Dim ELog As New EventLog(sLog, sMachine, sSource)
            ELog.WriteEntry(ex.ToString, EventLogEntryType.Error, 234, CType(3, Short))
            ToolStripStatusLabel2.Text = "Error detected. Please see Event Viewer for more details."
        End Try
    End Sub
    Sub sendMail()
        Dim ToEmail As String = My.Settings.ToEmail
        Dim FromEmail As String = My.Settings.FromEmail
        Dim attachments As String

        mail.From = New MailAddress(FromEmail)
        mail.To.Add(ToEmail)
        mail.Subject = "Active Directory User/Group Logs - " & timespan & " - " & Date.Today
        mail.Body = ""

        For Each Entry As String In My.Settings.Servers
            If My.Computer.FileSystem.FileExists(appdir & Entry & "_" & timespan.ToString("MM-dd-yyyy") & "-" & Date.Today.ToString("MM-dd-yyyy") & ".log") = True Then
                mail.Attachments.Add(New Attachment(appdir & Entry & "_" & timespan.ToString("MM-dd-yyyy") & "-" & Date.Today.ToString("MM-dd-yyyy") & ".log"))
                attachments += ControlChars.Tab & ControlChars.Tab & ControlChars.Tab & Entry & "_" & timespan.ToString("MM-dd-yyyy") & "-" & Date.Today.ToString("MM-dd-yyyy") & ".log" & Environment.NewLine
            Else
                errWriter.WriteLine(appdir & Entry & "_" & timespan.ToString("MM-dd-yyyy") & "-" & Date.Today.ToString("MM-dd-yyyy") & ".log - File not found.")
            End If

        Next
        Try
            smtp.Send(mail)
            Dim Now As DateTime = DateTime.Now

            sEvent = "Scan has completed." & Environment.NewLine
            sEvent += ControlChars.Tab & "Date:" & ControlChars.Tab & ControlChars.Tab & Date.Now & Environment.NewLine
            sEvent += ControlChars.Tab & "To:" & ControlChars.Tab & ControlChars.Tab & ToEmail & Environment.NewLine
            sEvent += ControlChars.Tab & "From:" & ControlChars.Tab & ControlChars.Tab & FromEmail & Environment.NewLine
            sEvent += ControlChars.Tab & "Logs Emailed:" & ControlChars.Tab & "Yes" & Environment.NewLine
            sEvent += ControlChars.Tab & "Attachments:" & ControlChars.Tab & ControlChars.Tab & Environment.NewLine
            sEvent += attachments
            sMachine = "."
            If Not EventLog.SourceExists(sSource, sMachine) Then
                EventLog.CreateEventSource(sSource, sLog, sMachine)
            End If
            Dim ELog As New EventLog(sLog, sMachine, sSource)
            ELog.WriteEntry(sEvent)
            ToolStripStatusLabel2.Text = "Last email sent at " & Date.Now
        Catch ex As Exception
            errWriter.WriteLine(Date.Now)
            errWriter.WriteLine(ex.ToString)
            errWriter.WriteLine("")
            Dim ELog As New EventLog(sLog, sMachine, sSource)
            ELog.WriteEntry(ex.ToString, EventLogEntryType.Error, 234, CType(3, Short))
            ToolStripStatusLabel2.Text = "Error detected. Please see Event Viewer for more details."
        End Try
        mail.Attachments.Clear()
        mail.To.Clear()
    End Sub
    Sub beginScan()
        For Each Entry As String In My.Settings.Servers
            getSecurity(Entry)
        Next
        sendMail()
        ToolStripStatusLabel1.Text = "Waiting..."
    End Sub
    Function emptyCheck()
        If Me.InvokeRequired Then
            Me.Invoke(New MethodInvoker(AddressOf emptyCheck))
        Else
            If String.IsNullOrEmpty(txtFromEmail.Text) Then
                fromempty = True
                txtFromEmail.BackColor = Color.Red
            End If
            If String.IsNullOrEmpty(txtToEmail.Text) Then
                toempty = True
                txtToEmail.BackColor = Color.Red
            End If
            If String.IsNullOrEmpty(txtInterval.Text) Then
                intervalempty = True
                txtInterval.BackColor = Color.Red
            End If
            If String.IsNullOrEmpty(txtSMTP.Text) Then
                smtpempty = True
                txtSMTP.BackColor = Color.Red
            End If
            If String.IsNullOrEmpty(txtDaysToScan.Text) Then
                emptyDaysToScan = True
                txtDaysToScan.BackColor = Color.Red
            End If
            If radMinutes.Checked = False Then
                If radDays.Checked = False Then
                    radMinUnchecked = True
                    radMinutes.BackColor = Color.Red
                End If
            End If
            If radDays.Checked = False Then
                If radMinutes.Checked = False Then
                    radDayUnchecked = True
                    radDays.BackColor = Color.Red
                End If
            End If
        End If

        If (fromempty = True) Or (toempty = True) Or (intervalempty = True) Or (smtpempty = True) Or ((radMinUnchecked = True And radDayUnchecked = True)) Or emptyDaysToScan = True Then
            MessageBox.Show("Invalid entry detected! Please correct highlighted entries.", "Invalid entry", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
            Return True
        Else
            If btnEdit.Text = "Save" Then
                Dim time As TimeSpan = Date.Now - DateTimePicker1.Value
                Dim seconds As Double = time.Seconds
                If seconds > 0 Then
                    MessageBox.Show("Invalid date detected! Please select a future date.", "Invalid date", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
                    Return True
                Else
                    Return False
                End If
            End If
            Return False
            End If

    End Function
    Function MinuteToMilliseconds(ByVal min As Double)
        Return min * 60000
    End Function
    Function DaysToMilliseconds(ByVal day As Double)
        Return day * 86400000
    End Function
    Private Sub btnStartStop_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnStartStop.Click
        If emptyCheck() = False Then
            If btnStartStop.Text = "Start Scheduled Scanning" Then
                btnStartStop.Text = "Stop"
                btnEdit.Enabled = False
                ToolStripStatusLabel1.Text = "Waiting..."
                smtp = New SmtpClient(My.Settings.SMTPServer)
                timespan = DateTime.Today.AddDays("-" & CInt(txtDaysToScan.Text))
                ToolStripStatusLabel5.Text = "Scan scheduled for " & DateTimePicker1.Text
                Timer2.Start()
            Else
                btnStartStop.Text = "Start Scheduled Scanning"
                ToolStripStatusLabel1.Text = "Stopped"
                ToolStripStatusLabel5.Text = "Next scan not scheduled"
                btnEdit.Enabled = True
                Timer1.Stop()
                Timer2.Stop()
                timerenabled = False
                Try
                    If trd.IsAlive() Then
                        trd.Abort()
                    End If
                Catch ex As Exception
                End Try
            End If
        End If
    End Sub
    Private Sub btnEdit_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnEdit.Click
        If btnEdit.Text = "Edit" Then
            btnEdit.Text = "Save"
            txtFromEmail.Enabled = True
            txtToEmail.Enabled = True
            txtInterval.Enabled = True
            txtSMTP.Enabled = True
            txtServers.Enabled = True
            btnStartStop.Enabled = False
            radDays.Enabled = True
            radMinutes.Enabled = True
            txtDaysToScan.Enabled = True
            DateTimePicker1.Enabled = True
        Else
            If emptyCheck() = False Then
                btnEdit.Text = "Edit"
                txtFromEmail.Enabled = False
                txtToEmail.Enabled = False
                txtInterval.Enabled = False
                txtSMTP.Enabled = False
                btnStartStop.Enabled = True
                txtServers.Enabled = False
                radMinutes.Enabled = False
                radDays.Enabled = False
                txtDaysToScan.Enabled = False
                DateTimePicker1.Enabled = False
                My.Settings.Servers.Clear()
                For Each i In txtServers.Lines
                    If My.Settings.Servers.Contains(i) Then
                    Else
                        If i = "" Then
                        Else
                            My.Settings.Servers.Add(i)
                        End If
                    End If
                Next
                My.Settings.ToEmail = txtToEmail.Text
                My.Settings.FromEmail = txtFromEmail.Text
                My.Settings.Interval = txtInterval.Text
                My.Settings.SMTPServer = txtSMTP.Text
                My.Settings.DaysToScan = txtDaysToScan.Text
                If radDays.Checked = True Then
                    My.Settings.IntervalLength = "Days"
                ElseIf radMinutes.Checked = True Then
                    My.Settings.IntervalLength = "Minutes"
                End If
            End If
        End If
    End Sub
    Private Sub OpenViewerToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles OpenViewerToolStripMenuItem.Click
        Dim frmViewer As New Viewer
        frmViewer.ShowDialog()
    End Sub
    Private Sub txtFromEmail_GotFocus(ByVal sender As Object, ByVal e As System.EventArgs) Handles txtFromEmail.GotFocus
        If fromempty = True Then
            fromempty = False
            txtFromEmail.BackColor = Color.Empty
        End If
    End Sub
    Private Sub txtInterval_GotFocus(ByVal sender As Object, ByVal e As System.EventArgs) Handles txtInterval.GotFocus
        If intervalempty = True Then
            intervalempty = False
            txtInterval.BackColor = Color.Empty
        End If
    End Sub
    Private Sub txtToEmail_GotFocus(ByVal sender As Object, ByVal e As System.EventArgs) Handles txtToEmail.GotFocus
        If toempty = True Then
            toempty = False
            txtToEmail.BackColor = Color.Empty
        End If
    End Sub

    Private Sub txtSMTP_GotFocus(ByVal sender As Object, ByVal e As System.EventArgs) Handles txtSMTP.GotFocus
        If smtpempty = True Then
            smtpempty = False
            txtSMTP.BackColor = Color.Empty
        End If
    End Sub
    Sub nextScan()
        If radDays.Checked = True Then
            ToolStripStatusLabel5.Text = "Next scan at " & Now.AddDays(txtInterval.Text)
        Else
            ToolStripStatusLabel5.Text = "Next scan at " & Now.AddMinutes(txtInterval.Text)
        End If
    End Sub
    Private Sub Timer1_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Timer1.Tick
        TimerEnabled = True
        If radMinutes.Checked = True Then
            Timer1.Interval = MinuteToMilliseconds(txtInterval.Text)
        End If
        If radDays.Checked = True Then
            Timer1.Interval = DaysToMilliseconds(txtInterval.Text)
        End If
        trd = New Thread(AddressOf beginScan)
        nextScan()
        trd.Start()
        ToolStripStatusLabel1.Text = "Running"
    End Sub
    Private Sub radDays_GotFocus(ByVal sender As Object, ByVal e As System.EventArgs) Handles radDays.GotFocus
        If radDayUnchecked = True Then
            radDayUnchecked = False
            radDays.BackColor = Color.Empty
            radMinUnchecked = False
            radMinutes.BackColor = Color.Empty
        End If
    End Sub

    Private Sub radMinutes_GotFocus(ByVal sender As Object, ByVal e As System.EventArgs) Handles radMinutes.GotFocus
        If radMinUnchecked = True Then
            radDayUnchecked = False
            radDays.BackColor = Color.Empty
            radMinUnchecked = False
            radMinutes.BackColor = Color.Empty
        End If
    End Sub

    Private Sub txtDaysToScan_GotFocus(ByVal sender As Object, ByVal e As System.EventArgs) Handles txtDaysToScan.GotFocus
        If emptyDaysToScan = True Then
            emptyDaysToScan = False
            txtDaysToScan.BackColor = Color.Empty
        End If
    End Sub
    Private Sub Timer2_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Timer2.Tick
        Dim time As TimeSpan = Date.Now - DateTimePicker1.Value
        Dim seconds As Double = time.Seconds
        Dim minutes As Double = time.Minutes
        Dim hours As Double = time.Hours
        timerenabled = True
        Label8.Text = hours & " - " & minutes & " - " & seconds
        If seconds = 0 And minutes = 0 And hours = 0 Then
            Timer1.Start()
            Timer2.Stop()
        End If
    End Sub

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click
        MsgBox(Date.Now & " - " & DateTimePicker1.Value)
    End Sub
End Class
