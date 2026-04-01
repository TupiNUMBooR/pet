#Requires AutoHotkey v2.0
#SingleInstance Force

CoordMode("Mouse", "Screen")
Persistent

FileInstall("assets\pet.png", A_Temp "\pet.png", 1)
FileInstall("assets\tray.ico", A_Temp "\tray.ico", 1)
spritePath := A_Temp "\pet.png"
trayIconPath := A_Temp "\tray.ico"

petW := 128
petH := 128

offsetX := 18
offsetY := -110

smooth := 0.11
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
petGui.BackColor := "000000"
WinSetTransColor("000000", petGui)

petGui.MarginX := 0
petGui.MarginY := 0
petGui.AddPicture("x0 y0 w" petW " h" petH, spritePath)
petGui.Show("x" Round(x) " y" Round(y) " w" petW " h" petH " NoActivate")

UpdatePet()
SetTimer(UpdatePet, frameMs)

UpdatePet() {
    global x, y, petW, petH, offsetX, offsetY, smooth, petGui

    MouseGetPos(&mx, &my)

    scale := A_ScreenDPI / 96

    targetX := (mx + offsetX) / scale
    targetY := (my + offsetY) / scale

    x += (targetX - x) * smooth
    y += (targetY - y) * smooth

    ; ToolTip("mx=" mx "`nmy=" my "`ntargetX=" targetX "`ntargetY=" targetY "`nx=" x "`ny=" y)

    petGui.Move(Round(x), Round(y), petW, petH)
}
