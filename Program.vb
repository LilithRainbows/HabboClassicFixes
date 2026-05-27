Imports System.IO
Imports Flazzy
Imports Flazzy.ABC
Imports Flazzy.ABC.AVM2
Imports Flazzy.ABC.AVM2.Instructions
Imports Flazzy.IO

Module Program

    Sub Main(args As String())
        Directory.SetCurrentDirectory(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName))
        Console.Title = "HabboPhotoFixer"
        Try
            Console.WriteLine("Searching client ...")
            Dim TargetSwf = GetClientPath()
            Console.WriteLine("Found air client version " & New DirectoryInfo(Path.GetDirectoryName(TargetSwf)).Name)
            Console.WriteLine("")
            Console.WriteLine("Patching client ...")
            Dim Flash = New FlashFile(TargetSwf)
            Flash.Disassemble()
            EnablePhotoFix(Flash.AbcFiles)
            Using fileStream = File.Open(TargetSwf, FileMode.Create)
                Using fileWriter = New FlashWriter(fileStream)
                    Console.WriteLine("")
                    Console.WriteLine("Saving edited client ...")
                    Flash.Assemble(fileWriter, CompressionKind.ZLib)
                End Using
            End Using
            Console.WriteLine("Ready! Restart the client to see the changes.")
        Catch ex As Exception
            Console.WriteLine("")
            Console.WriteLine("An error occurred:")
            Console.WriteLine("[" & ex.Message & "]")
            Console.WriteLine("")
        End Try
        Console.WriteLine("")
        Console.WriteLine("Press ENTER to exit")
        Do While Console.ReadKey(True).Key = ConsoleKey.Enter = False
            'Wait until user press ENTER
        Loop
        Environment.Exit(0)
    End Sub

    Function GetClientPath() As String
        Try
            Dim ClientPath As String = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) & "\Habbo Launcher\downloads\air"
            ClientPath += "\" & Directory.GetDirectories(ClientPath).Max(Function(d) New DirectoryInfo(d).Name) + "\HabboAir.swf"
            If File.Exists(ClientPath) Then
                Return ClientPath
            End If
        Catch
        End Try
        Throw New Exception("Client not found")
    End Function

    Function GetInstanceByRealName(ABCFiles As List(Of ABCFile), RealInstanceName As String) As ASInstance
        Try
            For Each ABCfile In ABCFiles
                For Each TempInstance In ABCfile.Instances
                    Try
                        Dim TempInstanceName As String = GetRealInstanceName(TempInstance)
                        If TempInstanceName = RealInstanceName Then
                            Return TempInstance
                        End If
                    Catch
                        'Failed to get real Instance name, skip to next
                    End Try
                Next
            Next
            Throw New Exception("Requested Instance not found")
        Catch
            Throw New Exception("Failed to get " & RealInstanceName & " Instance by real name")
        End Try
    End Function

    Function GetRealInstanceName(RequestedInstance As ASInstance) As String
        Try
            Dim RealInstanceName = RequestedInstance.Constructor.Name
            If String.IsNullOrWhiteSpace(RealInstanceName) Then
                Throw New Exception("Real Instance name cannot be empty")
            End If
            If RealInstanceName.StartsWith("_-") Then
                Throw New Exception("Invalid real Instance name")
            End If
            Return RealInstanceName
        Catch
            Throw New Exception("Failed to get " & RequestedInstance.QName.Name & " instance name")
        End Try
    End Function
    Function GetInstanceByName(ABCFiles As List(Of ABCFile), RequestedInstance As String) As ASInstance
        For Each ABCfile In ABCFiles
            Try
                Return ABCfile.Instances.First(Function(x) x.QName.Name = RequestedInstance)
            Catch
                Continue For
            End Try
        Next
        Throw New Exception("Instance (" & RequestedInstance & ") not found")
    End Function

    Sub EnablePhotoFix(ABCFiles As List(Of ABCFile))
        Try
            Dim UpdateModelMethod = GetInstanceByName(ABCFiles, GetInstanceByRealName(ABCFiles, "FurnitureExternalImageVisualization").Super.Name).GetMethod("updateModel")
            Dim MethodCode = UpdateModelMethod.Body.ParseCode()
            For i As Integer = 0 To MethodCode.Count - 1
                Dim CurrentInstruction = MethodCode(i)
                If CurrentInstruction.OP = OPCode.SetProperty AndAlso CType(CurrentInstruction, SetPropertyIns).PropertyName.Name = "checkPolicyFile" Then
                    If MethodCode(i + 2).OP = OPCode.PushString Then
                        Throw New Exception("Client already patched")
                    End If

                    Dim PhotoFixCodeBlock As New List(Of ASInstruction) From {
         New GetLocalIns(4),
         New PushStringIns(UpdateModelMethod.ABC, "_small.png"),
         New CallPropertyIns(UpdateModelMethod.ABC, AddToPoolAndGetIndex(UpdateModelMethod.ABC, "http://adobe.com/AS3/2006/builtin", "indexOf"), 1),
         New PushByteIns(0),
         New IfNotGreaterEqualIns(), 'Index: 5
         New FindPropStrictIns(UpdateModelMethod.ABC, AddToPoolAndGetIndex(UpdateModelMethod.ABC, "", "Number", NamespaceKind.Package)),
         New GetLocalIns(4),
         New GetLocalIns(4),
         New PushStringIns(UpdateModelMethod.ABC, "-"),
         New CallPropertyIns(UpdateModelMethod.ABC, AddToPoolAndGetIndex(UpdateModelMethod.ABC, "http://adobe.com/AS3/2006/builtin", "lastIndexOf"), 1),
         New PushByteIns(1),
         New AddIns(),
         New CallPropertyIns(UpdateModelMethod.ABC, AddToPoolAndGetIndex(UpdateModelMethod.ABC, "http://adobe.com/AS3/2006/builtin", "substring"), 1),
         New PushStringIns(UpdateModelMethod.ABC, "_small.png"),
         New PushStringIns(UpdateModelMethod.ABC, ""),
         New CallPropertyIns(UpdateModelMethod.ABC, AddToPoolAndGetIndex(UpdateModelMethod.ABC, "http://adobe.com/AS3/2006/builtin", "replace"), 2),
         New CallPropertyIns(UpdateModelMethod.ABC, AddToPoolAndGetIndex(UpdateModelMethod.ABC, "", "Number", NamespaceKind.Package), 1),
         New FindPropStrictIns(UpdateModelMethod.ABC, AddToPoolAndGetIndex(UpdateModelMethod.ABC, "", "Number", NamespaceKind.Package)),
         New PushStringIns(UpdateModelMethod.ABC, "1585699200000"),
         New CallPropertyIns(UpdateModelMethod.ABC, AddToPoolAndGetIndex(UpdateModelMethod.ABC, "", "Number", NamespaceKind.Package), 1),
         New IfNotGreaterThanIns, 'Index: 21
         New GetLocalIns(4),
         New PushStringIns(UpdateModelMethod.ABC, "_small.png"),
         New PushStringIns(UpdateModelMethod.ABC, ".png"),
         New CallPropertyIns(UpdateModelMethod.ABC, AddToPoolAndGetIndex(UpdateModelMethod.ABC, "http://adobe.com/AS3/2006/builtin", "replace"), 2),
         New CoerceSIns(),
         New SetLocalIns(4)
         }
                    MethodCode.InsertRange(i + 1, PhotoFixCodeBlock)
                    CType(MethodCode(i + 5), Jumper).Offset = GetBytesLengthBetweenCodeLines(MethodCode, i + 5, i + PhotoFixCodeBlock.Count)
                    CType(MethodCode(i + 21), Jumper).Offset = GetBytesLengthBetweenCodeLines(MethodCode, i + 21, i + PhotoFixCodeBlock.Count)
                    UpdateModelMethod.Body.MaxStack = 4
                    UpdateModelMethod.Body.MaxScopeDepth = 9
                    Exit For
                End If
            Next
            UpdateModelMethod.Body.Code = MethodCode.ToArray()
        Catch ex As Exception
            If ex.Message.Contains("patched") = False Then
                Throw New Exception("EnablePhotoFix failed")
            Else
                Throw
            End If
        End Try
    End Sub

    Function GetBytesLengthBetweenCodeLines(Code As ASCode, LineStart As Integer, LineEnd As Integer) As Integer
        Dim Length As Integer = 0
        For i As Integer = 0 To Code.Count - 1
            Dim CurrentInstruction = Code(i)
            If i > LineStart Then
                Length += CurrentInstruction.ToArray.Length
            End If
            If i = LineEnd Then
                Return Length
            End If
        Next
        Return 0
    End Function

    Function AddToPoolAndGetIndex(AbcFileDestination As ABCFile, ReferenceNamespace As String, ReferenceName As String, Optional NamespaceKind As NamespaceKind = NamespaceKind.Namespace) As Integer
        Try
            Dim newReferenceNamespace = New ASNamespace(AbcFileDestination.Pool) With {
      .NameIndex = AbcFileDestination.Pool.AddConstant(ReferenceNamespace),
      .Kind = NamespaceKind
  }
            Dim newReferenceNamespaceIndex As Integer = AbcFileDestination.Pool.AddConstant(newReferenceNamespace)
            Dim newReferenceName = New ASMultiname(AbcFileDestination.Pool) With {
                .Kind = MultinameKind.QName,
                .NamespaceIndex = newReferenceNamespaceIndex,
                .NameIndex = AbcFileDestination.Pool.AddConstant(ReferenceName)
            }
            Dim newReferenceNameIndex As Integer = AbcFileDestination.Pool.AddConstant(newReferenceName)
            Return newReferenceNameIndex
        Catch
            Throw New Exception("AddToPoolAndGetIndex failed")
        End Try
    End Function

End Module
