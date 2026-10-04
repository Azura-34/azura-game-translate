Unicode true
!include "MUI2.nsh"

Name "Azura Game Translate 1.0.0"
OutFile "dist\AzuraGameTranslate-Setup-v1.0.0.exe"
InstallDir "$PROGRAMFILES64\Azura Game Translate"
InstallDirRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\AzuraGameTranslate" "InstallLocation"
RequestExecutionLevel admin
SetCompressor /SOLID lzma
SetDatablockOptimize on
ShowInstDetails show
ShowUninstDetails show

VIProductVersion "1.0.0.0"
VIAddVersionKey "ProductName" "Azura Game Translate"
VIAddVersionKey "ProductVersion" "1.0.0"
VIAddVersionKey "FileDescription" "Azura Game Translate Windows Installer"
VIAddVersionKey "FileVersion" "1.0.0"
VIAddVersionKey "LegalCopyright" "Copyright (c) 2026 Azura-34"

!define MUI_ABORTWARNING
!define MUI_WELCOMEPAGE_TITLE "Azura Game Translate Kurulumu"
!define MUI_WELCOMEPAGE_TEXT "Oyun altyazılarını okuyup Türkçeye çeviren uygulamayı bilgisayarınıza kurar.$\r$\n$\r$\nKurulum, gerekli OCR bileşeni Tesseract'ı da ekler. Windows güvenlik penceresi açılırsa kuruluma izin verin.$\r$\n$\r$\nİlk kullanımda DeepL API anahtarı gerekir."
!define MUI_DIRECTORYPAGE_TEXT_TOP "Azura Game Translate hangi klasöre kurulsun?"
!define MUI_FINISHPAGE_TITLE "Kurulum tamamlandı"
!define MUI_FINISHPAGE_TEXT "Azura Game Translate kullanıma hazır. İlk açılışta DeepL API anahtarınızı girin."
!define MUI_FINISHPAGE_RUN "$INSTDIR\AzuraGameTranslate.exe"
!define MUI_FINISHPAGE_RUN_TEXT "Azura Game Translate'i şimdi başlat"
!define MUI_UNFINISHPAGE_TITLE "Kaldırma tamamlandı"

!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_LANGUAGE "Turkish"

Section "-Azura Game Translate" SEC_MAIN
  SetRegView 64
  SetShellVarContext all

  ; Var olan Tesseract kurulumu varsa tekrar kurma.
  nsExec::ExecToLog '"tesseract.exe" --version'
  Pop $0
  StrCmp $0 "0" tesseract_ready
  SetOutPath "$PLUGINSDIR"
  File /oname=TesseractSetup.exe ".build\TesseractSetup.exe"
  DetailPrint "OCR bileşeni kuruluyor..."
  ExecWait '"$PLUGINSDIR\TesseractSetup.exe" /S' $0
  StrCmp $0 "0" tesseract_ready
  MessageBox MB_ICONSTOP "OCR bileşeni kurulamadı. Kurulum durduruldu. Hata kodu: $0"
  Abort

tesseract_ready:
  SetOutPath "$INSTDIR"
  File /oname=AzuraGameTranslate.exe ".build\AzuraGameTranslate.exe"
  WriteUninstaller "$INSTDIR\Uninstall.exe"

  CreateDirectory "$SMPROGRAMS\Azura Game Translate"
  CreateShortcut "$SMPROGRAMS\Azura Game Translate\Azura Game Translate.lnk" "$INSTDIR\AzuraGameTranslate.exe"
  CreateShortcut "$SMPROGRAMS\Azura Game Translate\Kaldır.lnk" "$INSTDIR\Uninstall.exe"
  CreateShortcut "$DESKTOP\Azura Game Translate.lnk" "$INSTDIR\AzuraGameTranslate.exe"

  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\AzuraGameTranslate" "DisplayName" "Azura Game Translate"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\AzuraGameTranslate" "DisplayVersion" "1.0.0"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\AzuraGameTranslate" "Publisher" "Azura-34"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\AzuraGameTranslate" "InstallLocation" "$INSTDIR"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\AzuraGameTranslate" "UninstallString" '"$INSTDIR\Uninstall.exe"'
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\AzuraGameTranslate" "DisplayIcon" "$INSTDIR\AzuraGameTranslate.exe"
  WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\AzuraGameTranslate" "NoModify" 1
  WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\AzuraGameTranslate" "NoRepair" 1
SectionEnd

Section "Uninstall"
  SetRegView 64
  SetShellVarContext all
  Delete "$DESKTOP\Azura Game Translate.lnk"
  Delete "$SMPROGRAMS\Azura Game Translate\Azura Game Translate.lnk"
  Delete "$SMPROGRAMS\Azura Game Translate\Kaldır.lnk"
  RMDir "$SMPROGRAMS\Azura Game Translate"
  DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\AzuraGameTranslate"
  RMDir /r "$INSTDIR"
SectionEnd