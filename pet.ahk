#Requires AutoHotkey v2.0
#SingleInstance Force

CoordMode("Mouse", "Screen")
Persistent

spritePath := A_ScriptDir "\assets\pet.png"
trayIconPath := A_ScriptDir "\assets\tray.ico"

petW := 128
petH := 128

offsetX := 18
offsetY := -110

smooth := 0.22
frameMs := 16

x := 300.0
y := 300.0

A_IconTip := "Desktop Pet"

if FileExist(trayIconPath) {
    TraySetIcon(trayIconPath)
}

A_TrayMenu.Delete()
A_TrayMenu.Add("Exit", (*) => ExitApp())

petGui := Gui("+AlwaysOnTop -Caption +ToolWindow +E0x20 +LastFound")
petGui.BackColor := "FF00FF"
WinSetTransColor("FF00FF", petGui)

petGui.MarginX := 0
petGui.MarginY := 0
petGui.AddPicture("x0 y0 w" petW " h" petH, spritePath)
petGui.Show("x" Round(x) " y" Round(y) " w" petW " h" petH " NoActivate")

UpdatePet()
SetTimer(UpdatePet, frameMs)

UpdatePet() {
    global x, y, petW, petH, offsetX, offsetY, smooth, petGui

    MouseGetPos(&mx, &my)

    targetX := mx + offsetX
    targetY := my + offsetY

    x += (targetX - x) * smooth
    y += (targetY - y) * smooth

    petGui.Move(Round(x), Round(y), petW, petH)
}