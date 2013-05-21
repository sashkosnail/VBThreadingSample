Imports System.Threading

Public Class Form1

    'The buffers below should be frame objects
    'Array of arrays first level splits into old/new buffers, and then each element is for each camera frame buffer
    Dim buffers = {{0, 0}, {0, 0}}

    'Timeout Value
    Const timeout As Integer = 2250

    'Holding flags for camera communication timeouts
    Dim camera_timout = {False, False}

    'Holding errors for each camera thread
    Dim camera_error = {"", ""}

    Dim process_result As Integer = 0

    Dim random_number As New System.Random

    Dim keepRunning As Boolean = False

    Dim mainThread As New Thread(AddressOf Me.MainThreadfunc)

    'Error handling in the next two thread functions should be done through a memeber variable

    Private Sub CameraThreadfunc(ByVal iCamNumber As Integer)  'The integer identifier here should be replaced with the camera handle
        Thread.Sleep(CInt(1500.0 + random_number.NextDouble * 1000.0))    'Suspend thread for a random time from 1.5 to 2.5s
        buffers(0, iCamNumber) += 1    'increment frame count in buffer used in display
        If random_number.NextDouble > 0.99 Then camera_error(iCamNumber) = "ERROR"
    End Sub

    Private Sub ProcessThreadfunc(BufferID As Integer)
        Thread.Sleep(CInt(100.0 + random_number.NextDouble * 150.0))    'Suspend thread for a random time from 0.1 to 0.25s
        process_result = (buffers(1, 0) + buffers(1, 1)) '* random_number.NextDouble
    End Sub

    Private Sub MainThreadfunc()
        Dim CameraThread1 As New Thread(AddressOf Me.CameraThreadfunc)
        Dim CameraThread2 As New Thread(AddressOf Me.CameraThreadfunc)
        Dim ProcessThread As New Thread(AddressOf Me.ProcessThreadfunc)

        Dim updateUIDelegate = New MethodInvoker(AddressOf UpdateUI)

        Static notFirstRun As Boolean = False
        Try
            While keepRunning
                camera_timout = {False, False}
                'New set of threads needs to be created for each run when no using a thread pool
                CameraThread1 = New Thread(AddressOf Me.CameraThreadfunc)
                CameraThread2 = New Thread(AddressOf Me.CameraThreadfunc)
                ProcessThread = New Thread(AddressOf Me.ProcessThreadfunc)

                'Start all threads in background
                CameraThread1.IsBackground = True
                CameraThread1.Start(0)
                CameraThread2.IsBackground = True
                CameraThread2.Start(1)

                'Do not run PRocess on the first loop
                If notFirstRun Then
                    ProcessThread.IsBackground = True
                    ProcessThread.Start()
                Else
                    notFirstRun = True
                End If

                'Wait for all started threads to finish before starting new iteration
                If CameraThread1.IsAlive Then If CameraThread1.Join(timeout) Then camera_timout(0) = True
                If CameraThread1.IsAlive Then If CameraThread2.Join(timeout) Then camera_timout(1) = True

                If ProcessThread.IsAlive Then ProcessThread.Join()

                'Save new data into the processign layer of the buffers
                buffers(1, 0) = buffers(0, 0)
                buffers(1, 1) = buffers(0, 1)

                'Update the UI
                Invoke(updateUIDelegate)

                'Check if error occured and if so terminate thread
                If camera_error(0) <> "" Or camera_error(1) <> "" Then Invoke(New MethodInvoker(AddressOf Failsafe))
            End While
        Catch ex As Exception
            MsgBox(ex.Message)
        Finally
            'Cleanup ALWAYS RUNS
            If CameraThread1.IsAlive Then CameraThread1.Join()
            If CameraThread2.IsAlive Then CameraThread2.Join()
            If ProcessThread.IsAlive Then ProcessThread.Join()
        End Try
    End Sub

    Private Sub Failsafe()
        StartStop_Click(Nothing, Nothing)
    End Sub

    Private Sub UpdateUI()
        Try
            T1.Text = buffers(0, 0).ToString
            If camera_timout(0) Then T1.BackColor = Color.Red Else T1.BackColor = SystemColors.Control
            T2.Text = buffers(0, 1).ToString
            If camera_timout(1) Then T2.BackColor = Color.Red Else T2.BackColor = SystemColors.Control

            P.Text = process_result.ToString

            T1E.Text = camera_error(0)
            T2E.Text = camera_error(1)
        Catch ex As Exception
            MsgBox(ex.Message)
        End Try
    End Sub

    Private Sub StartStop_Click(sender As Object, e As EventArgs) Handles StartStop.Click
        If keepRunning Then
            StartStop.Text = "Start"
            StartStop.BackColor = Color.Green
            keepRunning = False
        Else
            StartStop.Text = "Stop"
            StartStop.BackColor = Color.Red
            keepRunning = True
            'Make sure previous run has cleared
            If mainThread.IsAlive Then mainThread.Join()
            mainThread = New Thread(AddressOf Me.MainThreadfunc)
            mainThread.IsBackground = True
            mainThread.Start()
        End If
    End Sub

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        mainThread.IsBackground = True
    End Sub
End Class
