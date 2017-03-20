Imports HttpServer

Module Module1

	Sub Main()
		Using server As New HttpServer.HttpServer
			server.RootPath = "api"
			server.HandleRequest = New SimpleResponse
			server.AuthenticationScheme = Net.AuthenticationSchemes.Anonymous
			'server.AuthorizeRequest = New AuthorizeGroup With {.Group = "test"}
			server.Startup() ' Start it running
			While True
				Console.WriteLine(String.Format("Server is {0}", IIf(server.IsRunning, IIf(server.IsPaused, "Paused", "Running"), "Stopped")))
				Console.WriteLine()
				Console.WriteLine("1. Start")
				Console.WriteLine("2. Stop")
				Console.WriteLine("3. Pause")
				Console.WriteLine("4. Resume")
				Console.WriteLine("9. Exit")
				Console.Write("> ")
				Dim request = Console.ReadLine()
				Console.Clear()
				If request = String.Empty OrElse Not IsNumeric(request) Then Continue While

				Select Case CInt(request)
					Case 1 : server.Startup()
					Case 2 : server.Shutdown()
					Case 3 : server.Pause()
					Case 4 : server.Resume()
					Case 9 : Exit While
				End Select
			End While
		End Using
	End Sub
End Module
