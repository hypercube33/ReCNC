; Copyright (c) The OpenRA Developers and Contributors
; This file is part of OpenRA.
;
;  OpenRA is free software: you can redistribute it and/or modify
;  it under the terms of the GNU General Public License as published by
;  the Free Software Foundation, either version 3 of the License, or
;  (at your option) any later version.
;
;  OpenRA is distributed in the hope that it will be useful,
;  but WITHOUT ANY WARRANTY; without even the implied warranty of
;  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
;  GNU General Public License for more details.
;
;  You should have received a copy of the GNU General Public License
;  along with OpenRA.  If not, see <https://www.gnu.org/licenses/>.

!include "MUI2.nsh"
!include "FileFunc.nsh"
!include "WordFunc.nsh"

Name "ReCnC"
OutFile "${OUTFILE}"

ManifestDPIAware true

Unicode True

Function .onInit
	!ifndef USE_PROGRAMFILES32
		SetRegView 64
	!endif
	ReadRegStr $INSTDIR HKLM "Software\ReCnC${SUFFIX}" "InstallDir"
	StrCmp $INSTDIR "" unset done
	unset:
	; ReCnC: Default to C:\Games\ReCnC instead of Program Files
	StrCpy $INSTDIR "C:\Games\ReCnC"
	done:
FunctionEnd

SetCompressor lzma
RequestExecutionLevel admin

!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE "${SRCDIR}\COPYING"
!insertmacro MUI_PAGE_DIRECTORY

!define MUI_STARTMENUPAGE_REGISTRY_ROOT "HKLM"
!define MUI_STARTMENUPAGE_REGISTRY_KEY "Software\ReCnC${SUFFIX}"
!define MUI_STARTMENUPAGE_REGISTRY_VALUENAME "Start Menu Folder"
!define MUI_STARTMENUPAGE_DEFAULTFOLDER "ReCnC"

Var StartMenuFolder
!insertmacro MUI_PAGE_STARTMENU Application $StartMenuFolder

!insertmacro MUI_PAGE_COMPONENTS
!insertmacro MUI_PAGE_INSTFILES

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_UNPAGE_FINISH

!insertmacro MUI_LANGUAGE "English"

!define RA_DISCORDID 699222659766026240
!define CNC_DISCORDID 699223250181292033
!define D2K_DISCORDID 712711732770111550

;***************************
;Section Definitions
;***************************
Section "-Reg" Reg

	; Installation directory
	WriteRegStr HKLM "Software\ReCnC${SUFFIX}" "InstallDir" $INSTDIR

	; Join server URL Scheme
	WriteRegStr HKLM "Software\Classes\openra-ra-${TAG}" "" "URL:Join OpenRA server"
	WriteRegStr HKLM "Software\Classes\openra-ra-${TAG}" "URL Protocol" ""
	WriteRegStr HKLM "Software\Classes\openra-ra-${TAG}\DefaultIcon" "" "$INSTDIR\ra.ico,0"
	WriteRegStr HKLM "Software\Classes\openra-ra-${TAG}\Shell\Open\Command" "" "$INSTDIR\RedAlert.exe Launch.URI=%1"

	WriteRegStr HKLM "Software\Classes\discord-${RA_DISCORDID}" "" "URL:Run game ${RA_DISCORDID} protocol"
	WriteRegStr HKLM "Software\Classes\discord-${RA_DISCORDID}" "URL Protocol" ""
	WriteRegStr HKLM "Software\Classes\discord-${RA_DISCORDID}\DefaultIcon" "" "$INSTDIR\ra.ico,0"
	WriteRegStr HKLM "Software\Classes\discord-${RA_DISCORDID}\Shell\Open\Command" "" "$INSTDIR\RedAlert.exe"

	WriteRegStr HKLM "Software\Classes\openra-cnc-${TAG}" "" "URL:Join OpenRA server"
	WriteRegStr HKLM "Software\Classes\openra-cnc-${TAG}" "URL Protocol" ""
	WriteRegStr HKLM "Software\Classes\openra-cnc-${TAG}\DefaultIcon" "" "$INSTDIR\cnc.ico,0"
	WriteRegStr HKLM "Software\Classes\openra-cnc-${TAG}\Shell\Open\Command" "" "$INSTDIR\TiberianDawn.exe Launch.URI=%1"

	WriteRegStr HKLM "Software\Classes\discord-${CNC_DISCORDID}" "" "URL:Run game ${CNC_DISCORDID} protocol"
	WriteRegStr HKLM "Software\Classes\discord-${CNC_DISCORDID}" "URL Protocol" ""
	WriteRegStr HKLM "Software\Classes\discord-${CNC_DISCORDID}\DefaultIcon" "" "$INSTDIR\cnc.ico,0"
	WriteRegStr HKLM "Software\Classes\discord-${CNC_DISCORDID}\Shell\Open\Command" "" "$INSTDIR\TiberianDawn.exe"

	WriteRegStr HKLM "Software\Classes\openra-d2k-${TAG}" "" "URL:Join OpenRA server"
	WriteRegStr HKLM "Software\Classes\openra-d2k-${TAG}" "URL Protocol" ""
	WriteRegStr HKLM "Software\Classes\openra-d2k-${TAG}\DefaultIcon" "" "$INSTDIR\d2k.ico,0"
	WriteRegStr HKLM "Software\Classes\openra-d2k-${TAG}\Shell\Open\Command" "" "$INSTDIR\Dune2000.exe Launch.URI=%1"

	WriteRegStr HKLM "Software\Classes\discord-${D2K_DISCORDID}" "" "URL:Run game ${D2K_DISCORDID} protocol"
	WriteRegStr HKLM "Software\Classes\discord-${D2K_DISCORDID}" "URL Protocol" ""
	WriteRegStr HKLM "Software\Classes\discord-${D2K_DISCORDID}\DefaultIcon" "" "$INSTDIR\d2k.ico,0"
	WriteRegStr HKLM "Software\Classes\discord-${D2K_DISCORDID}\Shell\Open\Command" "" "$INSTDIR\Dune2000.exe"

	; BEGIN ReCnC BUG-011 — Tiberian Sun launcher URI (no separate Discord rich-presence id bundled).
	WriteRegStr HKLM "Software\Classes\openra-ts-${TAG}" "" "URL:Join OpenRA server"
	WriteRegStr HKLM "Software\Classes\openra-ts-${TAG}" "URL Protocol" ""
	WriteRegStr HKLM "Software\Classes\openra-ts-${TAG}\DefaultIcon" "" "$INSTDIR\ts.ico,0"
	WriteRegStr HKLM "Software\Classes\openra-ts-${TAG}\Shell\Open\Command" "" "$INSTDIR\TiberianSun.exe Launch.URI=%1"
	; END ReCnC BUG-011

	; Remove obsolete file associations
	DeleteRegKey HKLM "Software\Classes\.orarep"
	DeleteRegKey HKLM "Software\Classes\OpenRA_replay"
	DeleteRegKey HKLM "Software\Classes\.oramod"
	DeleteRegKey HKLM "Software\Classes\OpenRA_mod"
	DeleteRegKey HKLM "Software\Classes\openra"

SectionEnd

Section "Game" GAME
	SectionIn RO

	RMDir /r "$INSTDIR\mods"
	SetOutPath "$INSTDIR\mods"
	File /r "${SRCDIR}\mods\common"
	File /r "${SRCDIR}\mods\common-content"
	File /r "${SRCDIR}\mods\cnc"
	File /r "${SRCDIR}\mods\cnc-content"
	File /r "${SRCDIR}\mods\d2k"
	File /r "${SRCDIR}\mods\d2k-content"
	File /r "${SRCDIR}\mods\ra"
	File /r "${SRCDIR}\mods\ra-content"
	; BEGIN ReCnC BUG-011
	File /r "${SRCDIR}\mods\ts"
	File /r "${SRCDIR}\mods\ts-content"
	; END ReCnC BUG-011

	SetOutPath "$INSTDIR"
	File "${SRCDIR}\*.exe"
	File "${SRCDIR}\*.dll.config"
	File "${SRCDIR}\*.dll"
	File "${SRCDIR}\*.ico"
	File "${SRCDIR}\*.deps.json"
	File "${SRCDIR}\*.runtimeconfig.json"
	File "${SRCDIR}\global mix database.dat"
	File "${SRCDIR}\IP2LOCATION-LITE-DB1.IPV6.BIN.ZIP"
	File "${SRCDIR}\VERSION"
	File "${SRCDIR}\AUTHORS"
	File "${SRCDIR}\COPYING"

	!insertmacro MUI_STARTMENU_WRITE_BEGIN Application
		CreateDirectory "$SMPROGRAMS\$StartMenuFolder"
		CreateShortCut "$SMPROGRAMS\$StartMenuFolder\Red Alert${SUFFIX}.lnk" $OUTDIR\RedAlert.exe "" \
			"$OUTDIR\RedAlert.exe" "" "" "" ""
		CreateShortCut "$SMPROGRAMS\$StartMenuFolder\Tiberian Dawn${SUFFIX}.lnk" $OUTDIR\TiberianDawn.exe "" \
			"$OUTDIR\TiberianDawn.exe" "" "" "" ""
		CreateShortCut "$SMPROGRAMS\$StartMenuFolder\Dune 2000${SUFFIX}.lnk" $OUTDIR\Dune2000.exe "" \
			"$OUTDIR\Dune2000.exe" "" "" "" ""
		; BEGIN ReCnC BUG-011
		CreateShortCut "$SMPROGRAMS\$StartMenuFolder\Tiberian Sun${SUFFIX}.lnk" $OUTDIR\TiberianSun.exe "" \
			"$OUTDIR\TiberianSun.exe" "" "" "" ""
		; END ReCnC BUG-011
	!insertmacro MUI_STARTMENU_WRITE_END

	SetOutPath "$INSTDIR\glsl"
	File "${SRCDIR}\glsl\*.frag"
	File "${SRCDIR}\glsl\*.vert"

	; Estimated install size for the control panel properties
	${GetSize} "$INSTDIR" "/S=0K" $0 $1 $2
	IntFmt $0 "0x%08X" $0
	WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\ReCnC${SUFFIX}" "EstimatedSize" "$0"

	SetShellVarContext all
	CreateDirectory "$APPDATA\OpenRA\ModMetadata"
	SetOutPath "$INSTDIR"
	nsExec::ExecToLog '"$INSTDIR\OpenRA.Utility.exe" ra --register-mod "$INSTDIR\RedAlert.exe" system'
	nsExec::ExecToLog '"$INSTDIR\OpenRA.Utility.exe" ra --clear-invalid-mod-registrations system'
	nsExec::ExecToLog '"$INSTDIR\OpenRA.Utility.exe" cnc --register-mod "$INSTDIR\TiberianDawn.exe" system'
	nsExec::ExecToLog '"$INSTDIR\OpenRA.Utility.exe" cnc --clear-invalid-mod-registrations system'
	nsExec::ExecToLog '"$INSTDIR\OpenRA.Utility.exe" d2k --register-mod "$INSTDIR\Dune2000.exe" system'
	nsExec::ExecToLog '"$INSTDIR\OpenRA.Utility.exe" d2k --clear-invalid-mod-registrations system'
	; BEGIN ReCnC BUG-011
	nsExec::ExecToLog '"$INSTDIR\OpenRA.Utility.exe" ts --register-mod "$INSTDIR\TiberianSun.exe" system'
	nsExec::ExecToLog '"$INSTDIR\OpenRA.Utility.exe" ts --clear-invalid-mod-registrations system'
	; END ReCnC BUG-011
	SetShellVarContext current

SectionEnd

Section "Desktop Shortcut" DESKTOPSHORTCUT
	SetOutPath "$INSTDIR"
	CreateShortCut "$DESKTOP\ReCnC - Red Alert${SUFFIX}.lnk" $INSTDIR\RedAlert.exe "" \
		"$INSTDIR\RedAlert.exe" "" "" "" ""
	CreateShortCut "$DESKTOP\ReCnC - Tiberian Dawn${SUFFIX}.lnk" $INSTDIR\TiberianDawn.exe "" \
		"$INSTDIR\TiberianDawn.exe" "" "" "" ""
	CreateShortCut "$DESKTOP\ReCnC - Dune 2000${SUFFIX}.lnk" $INSTDIR\Dune2000.exe "" \
		"$INSTDIR\Dune2000.exe" "" "" "" ""
	; BEGIN ReCnC BUG-011
	CreateShortCut "$DESKTOP\ReCnC - Tiberian Sun${SUFFIX}.lnk" $INSTDIR\TiberianSun.exe "" \
		"$INSTDIR\TiberianSun.exe" "" "" "" ""
	; END ReCnC BUG-011
SectionEnd

;***************************
;Uninstaller Sections
;***************************
Section "-Uninstaller"
	WriteUninstaller $INSTDIR\uninstaller.exe
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\ReCnC${SUFFIX}" "DisplayName" "ReCnC${SUFFIX}"
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\ReCnC${SUFFIX}" "UninstallString" "$INSTDIR\uninstaller.exe"
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\ReCnC${SUFFIX}" "QuietUninstallString" "$\"$INSTDIR\uninstaller.exe$\" /S"
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\ReCnC${SUFFIX}" "InstallLocation" "$INSTDIR"
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\ReCnC${SUFFIX}" "DisplayIcon" "$INSTDIR\ra.ico"
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\ReCnC${SUFFIX}" "Publisher" "OpenRA developers"
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\ReCnC${SUFFIX}" "URLInfoAbout" "https://openra.net"
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\ReCnC${SUFFIX}" "DisplayVersion" "${TAG}"
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\ReCnC${SUFFIX}" "NoModify" "1"
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\ReCnC${SUFFIX}" "NoRepair" "1"
SectionEnd

!macro Clean UN
Function ${UN}Clean
	nsExec::ExecToLog '"$INSTDIR\OpenRA.Utility.exe" ra --unregister-mod system'
	nsExec::ExecToLog '"$INSTDIR\OpenRA.Utility.exe" cnc --unregister-mod system'
	nsExec::ExecToLog '"$INSTDIR\OpenRA.Utility.exe" d2k --unregister-mod system'
	; BEGIN ReCnC BUG-011
	nsExec::ExecToLog '"$INSTDIR\OpenRA.Utility.exe" ts --unregister-mod system'
	; END ReCnC BUG-011

	RMDir /r $INSTDIR\mods
	RMDir /r $INSTDIR\maps
	RMDir /r $INSTDIR\glsl
	Delete $INSTDIR\*.exe
	Delete $INSTDIR\*.dll
	Delete $INSTDIR\*.ico
	Delete $INSTDIR\*.dll.config
	Delete $INSTDIR\*.deps.json
	Delete $INSTDIR\*.runtimeconfig.json
	Delete $INSTDIR\VERSION
	Delete $INSTDIR\AUTHORS
	Delete $INSTDIR\COPYING
	Delete "$INSTDIR\global mix database.dat"
	Delete $INSTDIR\IP2LOCATION-LITE-DB1.IPV6.BIN.ZIP

	RMDir /r $INSTDIR\Support

	!ifndef USE_PROGRAMFILES32
		SetRegView 64
	!endif

	DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\ReCnC${SUFFIX}"
	DeleteRegKey HKLM "Software\Classes\openra-ra-${TAG}"
	DeleteRegKey HKLM "Software\Classes\openra-cnc-${TAG}"
	DeleteRegKey HKLM "Software\Classes\openra-d2k-${TAG}"
	; BEGIN ReCnC BUG-011
	DeleteRegKey HKLM "Software\Classes\openra-ts-${TAG}"
	; END ReCnC BUG-011

	DeleteRegKey HKLM "Software\Classes\discord-${RA_DISCORDID}"
	DeleteRegKey HKLM "Software\Classes\discord-${CNC_DISCORDID}"
	DeleteRegKey HKLM "Software\Classes\discord-${D2K_DISCORDID}"

	Delete $INSTDIR\uninstaller.exe
	RMDir $INSTDIR

	!insertmacro MUI_STARTMENU_GETFOLDER Application $StartMenuFolder

	; Clean up start menu: Delete all our icons, and the ReCnC folder
	; *only* if we were the only installed version
	Delete "$SMPROGRAMS\$StartMenuFolder\Red Alert${SUFFIX}.lnk"
	Delete "$SMPROGRAMS\$StartMenuFolder\Tiberian Dawn${SUFFIX}.lnk"
	Delete "$SMPROGRAMS\$StartMenuFolder\Dune 2000${SUFFIX}.lnk"
	; BEGIN ReCnC BUG-011
	Delete "$SMPROGRAMS\$StartMenuFolder\Tiberian Sun${SUFFIX}.lnk"
	; END ReCnC BUG-011
	RMDir "$SMPROGRAMS\$StartMenuFolder"

	Delete "$DESKTOP\ReCnC - Red Alert${SUFFIX}.lnk"
	Delete "$DESKTOP\ReCnC - Tiberian Dawn${SUFFIX}.lnk"
	Delete "$DESKTOP\ReCnC - Dune 2000${SUFFIX}.lnk"
	; BEGIN ReCnC BUG-011
	Delete "$DESKTOP\ReCnC - Tiberian Sun${SUFFIX}.lnk"
	; END ReCnC BUG-011
	DeleteRegKey HKLM "Software\ReCnC${SUFFIX}"
FunctionEnd
!macroend

!insertmacro Clean ""
!insertmacro Clean "un."

Section "Uninstall"
	Call un.Clean
SectionEnd

;***************************
;Section Descriptions
;***************************
LangString DESC_GAME ${LANG_ENGLISH} "ReCnC engine, official mods and dependencies"
LangString DESC_DESKTOPSHORTCUT ${LANG_ENGLISH} "Place shortcut on the Desktop."

!insertmacro MUI_FUNCTION_DESCRIPTION_BEGIN
	!insertmacro MUI_DESCRIPTION_TEXT ${GAME} $(DESC_GAME)
	!insertmacro MUI_DESCRIPTION_TEXT ${DESKTOPSHORTCUT} $(DESC_DESKTOPSHORTCUT)
!insertmacro MUI_FUNCTION_DESCRIPTION_END

;***************************
;Callbacks
;***************************

Function .onInstFailed
	Call Clean
FunctionEnd
