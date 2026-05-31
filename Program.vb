Imports System.IO
Imports System.Text
Imports Flazzy
Imports Flazzy.ABC
Imports Flazzy.ABC.AVM2
Imports Flazzy.ABC.AVM2.Instructions
Imports Flazzy.IO
Imports Flazzy.Tags

Module Program

    Sub Main(args As String())
        Directory.SetCurrentDirectory(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName))
        Console.Title = "HabboClassicFixes"
        Dim Flash
        Try
            Console.WriteLine("Searching client ...")
            Dim TargetSwf = GetClientPath()
            Dim ClientVersion = New DirectoryInfo(Path.GetDirectoryName(TargetSwf)).Name
            Console.WriteLine("Found air client version " & ClientVersion)
            Console.WriteLine("")
            Console.WriteLine("Loading client ...")
            Console.WriteLine("")
            Flash = New FlashFile(TargetSwf)
            Flash.Disassemble()
            MainMenu(Flash, ClientVersion, TargetSwf)
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
    End Sub

    Sub MainMenu(Flash As FlashFile, ClientVersion As Integer, TargetSwf As String)
        Console.Clear()
        Console.WriteLine("Client version: " & ClientVersion)
        Console.WriteLine("")
        Console.WriteLine("Available options:")
        Console.WriteLine("1: Apply all fixes")
        Console.WriteLine("2: Apply online friend notifier disabler")
        Console.WriteLine("3: Apply photo visualization fixer")
        Console.WriteLine("4: Apply stack tile fixer")
        Console.WriteLine("5: Exit")
        Console.WriteLine("")
        Console.Write("Select an option: ")
        Dim ExitLoop = False
        Do While ExitLoop = False
            ExitLoop = True
            Select Case Console.ReadKey(True).Key
                Case ConsoleKey.D1, ConsoleKey.NumPad1
                    TryApplyPatches(Flash, TargetSwf)
                Case ConsoleKey.D2, ConsoleKey.NumPad2
                    TryApplyPatches(Flash, TargetSwf, 2)
                Case ConsoleKey.D3, ConsoleKey.NumPad3
                    TryApplyPatches(Flash, TargetSwf, 3)
                Case ConsoleKey.D4, ConsoleKey.NumPad4
                    TryApplyPatches(Flash, TargetSwf, 4)
                Case ConsoleKey.D5, ConsoleKey.NumPad5
                    Process.GetCurrentProcess().Kill()
                Case Else
                    ExitLoop = False
            End Select
        Loop
        Console.WriteLine("")
        Console.WriteLine("Available options: ")
        Console.WriteLine("1: Return to main menu")
        Console.WriteLine("2: Exit")
        Console.WriteLine("")
        Console.Write("Select an option: ")
        ExitLoop = False
        Do While ExitLoop = False
            ExitLoop = True
            Select Case Console.ReadKey(True).Key
                Case ConsoleKey.D1, ConsoleKey.NumPad1
                    MainMenu(Flash, ClientVersion, TargetSwf)
                Case ConsoleKey.D2, ConsoleKey.NumPad2
                    Process.GetCurrentProcess().Kill()
                Case Else
                    ExitLoop = False
            End Select
        Loop
    End Sub

    Sub TryApplyPatches(Flash As FlashFile, TargetSwf As String, Optional RequestedPatch As Integer = 1)
        Console.Clear()
        Dim patches As New Dictionary(Of Integer, List(Of (Name As String, Action As Action))) From {
        {1, New List(Of (String, Action)) From {
            ("DisableNotifyFriendOnline", Sub() DisableNotifyFriendOnline(Flash.AbcFiles)),
            ("EnablePhotoFix", Sub() EnablePhotoFix(Flash.AbcFiles)),
            ("EnableStackTileFix", Sub() EnableStackTileFix(Flash))
        }},
        {2, New List(Of (String, Action)) From {
            ("DisableNotifyFriendOnline", Sub() DisableNotifyFriendOnline(Flash.AbcFiles))
        }},
        {3, New List(Of (String, Action)) From {
            ("EnablePhotoFix", Sub() EnablePhotoFix(Flash.AbcFiles))
        }},
        {4, New List(Of (String, Action)) From {
            ("EnableStackFix", Sub() EnableStackTileFix(Flash))
        }}
    }
        Dim AppliedPatches As Integer = 0
        If patches.ContainsKey(RequestedPatch) Then
            For Each patch In patches(RequestedPatch)
                Try
                    Console.WriteLine($"Applying {patch.Name} ...")
                    patch.Action.Invoke()
                    AppliedPatches += 1
                    Console.WriteLine("")
                Catch ex As Exception
                    Console.WriteLine($"[{ex.Message}]")
                    Console.WriteLine("")
                End Try
            Next
        End If
        If AppliedPatches > 0 Then
            Using fileStream = File.Open(TargetSwf, FileMode.Create)
                Using fileWriter = New FlashWriter(fileStream)
                    Console.WriteLine("Saving edited client ...")
                    Flash.Assemble(fileWriter, CompressionKind.ZLib)
                End Using
            End Using
            Console.WriteLine("Ready! Restart the client to see the changes.")
        Else
            Console.WriteLine("No patches could be applied!")
        End If
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

    Function GetCustomStackHeightXML() As String
        Return "<?xml version='1.0' encoding='UTF-8'?><layout name='custom_stack_height' width='320' height='210' version='0.1' uid='F92D997A-6BCC-A80E-4652-651E6875E987'><window><frame x='8' y='8' width='320' height='210' params='32769' style='100' caption='%24%7Bwidget.custom.stack.height.title%7D' height_min='185' height_max='210'><filters><DropShadowFilter angle='0' alpha='0.34901960784313724' blurX='20' blurY='20'/></filters><children><button x='12' y='110' width='134' height='29' params='131089' style='102' name='button_above_stack' caption='%24%7Bfurniture.above.stack%7D'/><button x='182' y='110' width='126' height='29' params='393233' style='102' name='button_floor_level' caption='%24%7Bfurniture.floor.level%7D'/><border x='35' y='68' width='188' height='30' params='17' style='105' name='slider'><children><container_button x='0' y='0' width='20' height='30' params='33073' style='102' name='slider_button'/></children></border><text x='10' y='5' width='294' height='59' params='16' style='100' name='height_text' caption='%24%7Bwidget.custom.stack.height.text%7D'><variables><var key='mouse_wheel_enabled' value='false' type='Boolean'/><var key='word_wrap' value='true' type='Boolean'/><var key='spacing' value='0' type='Number'/><var key='leading' value='0' type='Number'/></variables></text><border x='232' y='68' width='58' height='30' params='16' style='105'><children><input x='7' y='7' width='45' height='20' params='1' style='100' name='input_height' caption='1.0'><variables><var key='mouse_wheel_enabled' value='false' type='Boolean'/><var key='restrict' value='0123456789.' type='String'/><var key='spacing' value='0' type='Number'/><var key='leading' value='0' type='Number'/></variables></input></children></border><container x='0' y='149' width='318' height='24' params='16' style='3' name='walktile_container'><children><checkbox x='13' y='3' width='17' height='16' params='17' style='102' name='multiwalk_checkbox'/><text x='31' y='2' width='282' height='19' params='16' style='100' caption='%24%7Bwidget.custom.multiwalk_mode.text%7D'><variables><var key='mouse_wheel_enabled' value='false' type='Boolean'/><var key='word_wrap' value='true' type='Boolean'/><var key='spacing' value='0' type='Number'/><var key='leading' value='0' type='Number'/></variables></text></children></container><container_button x='9' y='84' width='19' height='20' params='1' style='102' dynamic_style='button' name='button_move_down'><children><icon x='5' y='5' width='12' height='12' params='0' style='0' color='0x07f7f7f' tags='%23icon'/></children><variables><var key='tool_tip_caption' value='${widget.custom.height.move_down}' type='String'/></variables></container_button><container_button x='9' y='62' width='19' height='20' params='1' style='102' dynamic_style='button' name='button_move_up'><children><icon x='5' y='4' width='12' height='12' params='0' style='1' color='0x07f7f7f' tags='%23icon'/></children><variables><var key='tool_tip_caption' value='${widget.custom.height.move_up}' type='String'/></variables></container_button><checkbox x='296' y='75' width='17' height='16' params='17' style='102' name='keep_height_checkbox'><variables><var key='tool_tip_caption' value='Keep height value' type='String'/></variables></checkbox><icon x='296' y='60' width='14' height='14' params='0' style='45' tags='%23icon'/></children></frame></window></layout>".Replace("'", Chr(34))
    End Function

    Sub EnableStackTileFix(Flash As FlashFile)
        Try
            Dim SymbolClassTags = Flash.Tags.Where(Function(t) t.Kind = TagKind.SymbolClass).Cast(Of SymbolClassTag)()
            Dim BinaryDataTags = Flash.Tags.Where(Function(t) t.Kind = TagKind.DefineBinaryData).Cast(Of DefineBinaryDataTag)()
            For Each BinaryDataTag In BinaryDataTags
                Dim SymbolClassTag = SymbolClassTags.First(Function(t) t.Ids.Contains(BinaryDataTag.Id))
                Dim BinaryDataName = SymbolClassTag.Names(SymbolClassTag.Ids.IndexOf(BinaryDataTag.Id))
                If BinaryDataName.Contains("custom_stack_height_xml") Then
                    BinaryDataTag.Data = Encoding.UTF8.GetBytes(GetCustomStackHeightXML)
                    Exit For
                End If
            Next
            Dim UpdateModelMethod = GetInstanceByRealName(Flash.AbcFiles, "CustomStackHeightWidget").GetMethod("canApplyLiveHeight")
            Dim MethodCode = UpdateModelMethod.Body.ParseCode()
            If MethodCode(3).OP = OPCode.Not = False Then
                Throw New Exception("Client already patched")
            End If
            Dim PhotoFixCodeBlock As New List(Of ASInstruction) From {
            New GetLexIns(UpdateModelMethod.ABC, AddToPoolAndGetIndex(UpdateModelMethod.ABC, "com.sulake.habbo.ui.widget.furniture:CustomStackHeightWidget", "_window", NamespaceKind.Private)),
            New PushStringIns(UpdateModelMethod.ABC, "keep_height_checkbox"),
            New CallPropertyIns(UpdateModelMethod.ABC, AddToPoolAndGetIndex(UpdateModelMethod.ABC, "com.sulake.core.window:IWindow", "findChildByName"), 1),
            New GetPropertyIns(UpdateModelMethod.ABC, AddToPoolAndGetIndex(UpdateModelMethod.ABC, "com.sulake.core.window.components:ISelectableWindow", "isSelected")),
            New IfFalseIns(), 'Index: 5
            New PushFalseIns(),
            New ReturnValueIns()
            }
            Dim StartLineIndex = 1
            MethodCode.InsertRange(StartLineIndex + 1, PhotoFixCodeBlock)
            CType(MethodCode(5 + StartLineIndex), Jumper).Offset = GetBytesLengthBetweenCodeLines(MethodCode, 5 + StartLineIndex, PhotoFixCodeBlock.Count + StartLineIndex)
            UpdateModelMethod.Body.Code = MethodCode.ToArray()
        Catch ex As Exception
            If ex.Message.Contains("patched") = False Then
                Throw New Exception("EnableStackFix failed")
            Else
                Throw
            End If
        End Try
    End Sub

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


    Sub DisableNotifyFriendOnline(ABCFiles As List(Of ABCFile))
        Try
            Dim NotifyFriendOnlineMethod = GetInstanceByRealName(ABCFiles, "FriendCategories").GetMethod("notifyFriendOnline")
            Dim MethodCode = NotifyFriendOnlineMethod.Body.ParseCode()
            If MethodCode(2).OP = OPCode.ReturnVoid Then
                Throw New Exception("Client already patched")
            Else
                MethodCode(2) = New ReturnVoidIns
                NotifyFriendOnlineMethod.Body.Code = MethodCode.ToArray
            End If
            'ClientMain.Constructor.Body.Code = ClientMainCode.ToArray
        Catch ex As Exception
            If ex.Message.Contains("patched") = False Then
                Throw New Exception("DisableNotifyFriendOnline failed")
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
