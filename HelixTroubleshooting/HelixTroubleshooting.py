import os, shutil, zipfile, pickle, time, subprocess, sys
import tkinter as tk
from tkinter import filedialog, messagebox
from PySide2.QtCore import *
from PySide2.QtGui import *
from PySide2.QtWidgets import *
from importlib import reload
from importlib.machinery import SourceFileLoader

testdirectory = 'C:\\Users\\grobinson\\Documents\\testdump\\Icon testing\\'\
#Config file loading-----------------------------------------------------------------------------------------------------------------------
try:
    config = SourceFileLoader('HTconfig','./HTconfig.py').load_module()
except FileNotFoundError:
    with open('HTconfig.py','wt',encoding = "UTF-8") as cfg:
        lines = [
                    "\n",
                    'rectImages  = r"\\\\universal-pc\RectImages2\"\n',
                    "\n",
                    "#Prefix directory strings with r\n",
                    '\n',
                    '#Location of HTools Database and Results Folders\n',
                    'HToolsDir = r"\\\\castor\Production\Manufacturing\Solo\Analysis Results"\n',
                    '\n',
                    '#Rectdata folder\n',
                    'rectData = r"\\\\castor\Ftproot\RectData\"\n',
                    '\n',
                    '#Tcomp results folder\n',
                    'tcompDir = r"\\\\castor\Production\Manufacturing\MfgSoftware\ThermalTest\\200-0526\Results\"\n',
                    '\n',
                    '#Tcomp backup folder\n',
                    'tcompBackup = r"\\\\castor\Production\Manufacturing\Evo\Tcomp Templates\Originals\"\n',
                    '\n',
                    'lineThresholdPercent = 20\n'
                ]
        for line in lines:
            cfg.write(line)
    config = SourceFileLoader('HTconfig','HTconfig.py').load_module()
import TTools
def reloadConfig():
    try:
        config = SourceFileLoader('HTconfig','./HTconfig.py').load_module()
    except Exception as e:
        TTools.errorMessage('Error!',str(e))
if not os.path.isfile(config.HToolsDir+'\\LineAnalysis.log'):
    with open(config.HToolsDir+'\\LineAnalysis.log','wt') as log:
        log.write(
            "SN\tPN\tDate\tSensor Rev\tRect Rev\tRect Pos Rev\tMin Line Width\tMax Line Width\t"
            "Near Focus\tStandoff Focus\tFar Focus\tLine Angle Deg\tThreshold %")


def changeDir():
    root = tk.Tk()
    root.withdraw()
    direc = filedialog.askdirectory()
    directory = direc.replace('/','\\') + '\\'
    if directory != '':
        return directory
    root.destroy()
    return

#Function for images included in pyinstaller .exe
def resource_path(relative_path):
    if hasattr(sys, '_MEIPASS'):
        return os.path.join(sys._MEIPASS, relative_path)
    return os.path.join(os.path.abspath("."), relative_path)

qt_app = QApplication(sys.argv)

class MainWindow(QMainWindow):
    def __init__(self):
        #Initialize the parent class, then set title and min window size
        super().__init__()
        self.setWindowTitle('Helix Troubleshooting 1.0.0')
        self.setMinimumSize(750,500)

        #Icon
        self.icon = QIcon(resource_path('images\\bar.png'))
        self.setWindowIcon(self.icon)
        #Set QWidget as central for the QMainWindow, QWidget will hold layouts
        self.centralwidget = QWidget()
        self.setCentralWidget(self.centralwidget)

        #Create the layouts and widgets----------------------------------------------------------------------------------------------------
        self.layout = QGridLayout()
        #Left Side of Window
        self.listLayout = QVBoxLayout()
        self.functionList = QListWidget()
        self.functions = sorted(['ALS Point Removal','Staring Dot Removal','Illuminated Sphere Summary','Fix Algorithm Errors','Solo Laser Line Analysis','Temperature Adjust'])
        self.functionList.addItems(self.functions)
        self.listLayout.addWidget(self.functionList)
        #Right side of window
        self.detailsLayout = QGridLayout()
        self.descriptionBox = QGroupBox("Function Description")
        self.groupBoxLayout = QVBoxLayout()
        self.description = QLabel()
        self.description.setWordWrap(True)
        self.description.setText('Helix Tools 0.3.0')
        self.parameters = QFormLayout()
        self.textEntry = QLineEdit()
        self.textEntry.setVisible(False)
        self.textEntry2 = QLineEdit()
        self.textEntry2.setVisible(False)
        self.functionButtons = QGridLayout()
        self.button1 = QPushButton('')
        self.button1.setVisible(False)
        self.button1.clicked.connect(self.button1Function)
        self.button2 = QPushButton('')
        self.button2.setVisible(False)
        self.button2.clicked.connect(self.button2Function)
        self.button3 = QPushButton('')
        self.button3.setVisible(False)
        self.button4 = QPushButton('')
        self.button4.setVisible(False)
        self.functionButtons.setAlignment(Qt.AlignLeft)
        self.functionButtons.addWidget(self.button1,0,0)
        self.functionButtons.addWidget(self.button2,0,1)
        self.functionButtons.addWidget(self.button3,0,2)
        self.functionButtons.addWidget(self.button4,0,3)
        self.groupBoxLayout.addWidget(self.description)
        self.descriptionBox.setLayout(self.groupBoxLayout)
        self.detailsLayout.addWidget(self.descriptionBox,0,0)
        self.detailsLayout.addLayout(self.parameters,1,0)
        self.detailsLayout.addWidget(self.textEntry,2,0)
        self.detailsLayout.addWidget(self.textEntry2,3,0)
        self.detailsLayout.addLayout(self.functionButtons,4,0)
        #Add sub-layouts to main layout 
        self.layout.addLayout(self.listLayout,0,0)
        self.layout.addLayout(self.detailsLayout,0,1)
        #Stretches and formatting
        self.layout.setColumnStretch(1,1)
        self.detailsLayout.setRowStretch(1,1)
        self.detailsLayout.setRowStretch(2,1)
        #Connections that need to be placed last due to object initialization
        self.functionList.currentItemChanged.connect(self.itemSelection)
        self.functionList.setCurrentRow(0)

        #Menu Bar--------------------------------------------------------------------------------------------------------------------------
        self.menubar = self.menuBar()
        self.exitAction = QAction("Exit",self)
        self.settingsAction = QAction("Settings", self)
        self.settingsAction.triggered.connect(self.settingsClick)
        self.filemenu = self.menubar.addMenu("&File")
        self.filemenu.addAction(self.exitAction)
        self.filemenu.addAction(self.settingsAction)

        #Set Layout
        self.centralwidget.setLayout(self.layout)
    #Child windows-------------------------------------------------------------------------------------------------------------------------
    def settingsWindow(self):
        self.settings = QWidget()
        self.settings.setMinimumSize(600,300)
        self.settings.setWindowTitle('Helix Trouble Tool Settings')
        self.settings.layout = QVBoxLayout()
        self.settings.form_layout = QFormLayout()
        self.settings.bottomButtons = QHBoxLayout()
        #----Blank label to add space between items
        self.settings.blankSpace = QLabel('')
        #----Buttons and text boxes for settings window
        self.settings.HToolsDirecEdit = QLineEdit(self)
        self.settings.HToolsDirecBtn = QPushButton('Browse')
        self.settings.HToolsDirecBtn.clicked.connect(self.toolsDirecClick)
        self.settings.dataDirecEdit = QLineEdit(self)
        self.settings.dataDirecBtn = QPushButton('Browse')
        self.settings.dataDirecBtn.clicked.connect(self.dataDirecClick)
        self.settings.imageDirecEdit = QLineEdit(self)
        self.settings.imageDirecBtn = QPushButton('Browse')
        self.settings.imageDirecBtn.clicked.connect(self.imageDirecClick)
        self.settings.tcompDirecEdit = QLineEdit(self)
        self.settings.tcompDirecBtn = QPushButton('Browse')
        self.settings.tcompDirecBtn.clicked.connect(self.tcompDirecClick)
        self.settings.backupDirecEdit = QLineEdit(self)
        self.settings.backupDirecBtn = QPushButton('Browse')
        self.settings.backupDirecBtn.clicked.connect(self.backupDirecClick)

        self.settings.okBtn = QPushButton('OK')
        self.settings.okBtn.clicked.connect(self.settingsOk)
        self.settings.cancelBtn = QPushButton('Cancel')
        self.settings.cancelBtn.clicked.connect(self.settingsCancel)
        self.settings.form_layout.addRow('HTools Data Directory',self.settings.HToolsDirecEdit)
        self.settings.form_layout.addRow('',self.settings.HToolsDirecBtn)
        self.settings.form_layout.addRow('',self.settings.blankSpace)
        self.settings.form_layout.addRow('RectData Directory',self.settings.dataDirecEdit)
        self.settings.form_layout.addRow('',self.settings.dataDirecBtn)
        self.settings.form_layout.addRow('',self.settings.blankSpace)
        self.settings.form_layout.addRow('RectImages Directory',self.settings.imageDirecEdit)
        self.settings.form_layout.addRow('',self.settings.imageDirecBtn)
        self.settings.form_layout.addRow('',self.settings.blankSpace)
        self.settings.form_layout.addRow('TComp Files Directory',self.settings.tcompDirecEdit)
        self.settings.form_layout.addRow('',self.settings.tcompDirecBtn)
        self.settings.form_layout.addRow('',self.settings.blankSpace)
        self.settings.form_layout.addRow('TComp Backup Directory',self.settings.backupDirecEdit)
        self.settings.form_layout.addRow('',self.settings.backupDirecBtn)
        self.settings.layout.addLayout(self.settings.form_layout)
        self.settings.layout.addStretch(1)
        self.settings.bottomButtons.addWidget(self.settings.okBtn)
        self.settings.bottomButtons.addWidget(self.settings.cancelBtn)
        self.settings.layout.addLayout(self.settings.bottomButtons)
        self.settings.setLayout(self.settings.layout)

    #Slots for buttons and main functions--------------------------------------------------------------------------------------------------
    @Slot()
    def itemSelection(self):
        self.textEntry.setVisible(False)
        self.textEntry2.setVisible(False)
        self.button1.setVisible(False)
        self.button2.setVisible(False)
        self.button3.setVisible(False)
        self.button4.setVisible(False)
        self.textEntry.clear()
        item = self.functionList.currentItem().text()
        if item == 'ALS Point Removal':
            self.description.setText('Removes erroneous entries in the CG text files for Evo sensors that cause ALS Point errors during rectification.\nEnter the directory of a rectification images folder, then click "Start".')
            self.textEntry.setVisible(True)
            self.button1.setText("Start")
            self.button1.setVisible(True)
        elif item == 'Staring Dot Removal':
            self.description.setText('Cleans the staring dots from rectification images of Evo sensors, providing a workaround for laser model errors caused by these dots.\nEnter the directory of a rectification images folder, then click "Start".')
            self.textEntry.setVisible(True)
            self.textEntry2.setVisible(False)
            self.button1.setText("Start")
            self.button1.setVisible(True)

        elif item == 'Illuminated Sphere Summary':
            self.description.setText(
                """Generates compilation images of illuminated spheres ("RZ" prefix) from the rectification images folder of Evo or Solo sensors. The summary images are placed in a folder named "Summary".\n\nEnter the directory of a rectification images folder, then click "Start".""")
            self.textEntry.setVisible(True)
            self.textEntry2.setVisible(False)
            self.button1.setText("Start")
            self.button1.setVisible(True)

        elif item == 'Fix Algorithm Errors':
            self.description.setText('Patches algorithm errors in an Evo sensor\'s t-comp file.\nEnter the serial number (SNXXXXXX) and click "Start".\n\nAlternatively, an original t-comp file can be replaced by clicking "Restore Original"')
            self.textEntry.setVisible(True)
            self.textEntry2.setVisible(False)
            self.button1.setText("Start")
            self.button1.setVisible(True)
            self.button2.setText("Restore Original")
            self.button2.setVisible(True)

        elif item == 'Solo Laser Line Analysis':
            self.description.setText('Analyzes the laser line images from the rectification images folder of a Helix Solo sensor. The results are placed in the analysis folder, specified in the config.\n\nEnter a directory for rectification images and click "Start".\n\nAlternatively, generate data for all Solo sensors in the config directories by clicking "Analyze All"')
            self.textEntry.setVisible(True)
            self.textEntry2.setVisible(False)
            self.button1.setText("Start")
            self.button1.setVisible(True)
            self.button2.setText("Analyze All")
            self.button2.setVisible(True)
        elif item == 'Day2 Calc':
            self.description.setText('Number of Solo sensors that require 2nd thermal cycle.')
            self.textEntry.setVisible(True)
            self.textEntry2.setVisible(False)
            self.button1.setText("Start")
            self.button1.setVisible(True)
            self.button2.setVisible(False)
        elif item == 'Temperature Adjust':
            self.description.setText("Adjust the temperature column of a t-comp file, so that the reference cycle average is equal to the sensor's current operating temperature.\n\nEnter the serial number (SNXXXXXX) in the first box, the desired reference average in the 2nd box, then click \"start\"\n\nUse the \"Restore Last\" button to restore the previous saved t-comp copy for the sensor.")
            self.textEntry.setVisible(True)
            self.textEntry2.setVisible(True)
            self.button1.setText("Start")
            self.button1.setVisible(True)
            self.button2.setText("Restore Last")
            self.button2.setVisible(True)
        else:
            self.description.setText('Helix Tools 0.3.0')

    @Slot()
    def button1Function(self):
        item = self.functionList.currentItem().text()
        text = self.textEntry.text()
        text2 = self.textEntry2.text()
        if item == 'ALS Point Removal':
            TTools.pointRemover(text)
        elif item == 'Staring Dot Removal':
            TTools.dotEraser(text)
        elif item == 'Illuminated Sphere Summary':
            TTools.summary(text)
        elif item == 'Fix Algorithm Errors':
            TTools.fixAlgoErrors(text,config.tcompDir,config.tcompBackup)
        elif item == 'Solo Laser Line Analysis':
            TTools.lineAnalysis(text, config.lineThresholdPercent,False,config.HToolsDir+'\\LineAnalysis.log')
        elif item == 'Day2 Calc':
            TTools.day2(text)
        elif item == 'Temperature Adjust':
            TTools.tempAdjust(text,text2,config.tcompDir,config.tcompBackup)
        else:
            pass

    @Slot()
    def button2Function(self):
        item = self.functionList.currentItem().text()
        text = self.textEntry.text()
        if item == 'Fix Algorithm Errors' or item == 'Temperature Adjust':
            TTools.restoreTcomp(text,otcompdir,backupdir)
        elif item == 'Solo Laser Line Analysis':
            TTools.lineAnalysis(text, config.lineThresholdPercent,True,config.HToolsDir+'\\LineAnalysis.log')
    #Methods for view changes based on function selection----------------------------------------------------------------------------------
    def alsRemovalView(self):
        pass
    def dotRemovalView(self):
        pass
    def sphereSummaryView(self):
        pass
    def algoErrorView(self):
        pass
    def lineAnalysisView(self):
        pass

    @Slot()
    def settingsClick(self):
        #Display current set directories
        self.settings.HToolsDirecEdit.setText(config.HToolsDir)
        self.settings.dataDirecEdit.setText(config.rectData)
        self.settings.imageDirecEdit.setText(config.rectImages)
        self.settings.tcompDirecEdit.setText(config.tcompDir)
        self.settings.backupDirecEdit.setText(config.tcompBackup)

        self.settings.show()

    #Settings button slots
    @Slot()
    def toolsDirecClick(self):
        directory = changeDir()
        if directory != None:
            self.settings.dataDirecEdit.setText(directory)
    @Slot()
    def dataDirecClick(self):
        directory = changeDir()
        if directory != None:
            self.settings.dataDirecEdit.setText(directory)
    @Slot()
    def imageDirecClick(self):
        directory = changeDir()
        if directory != None:
            self.settings.imageDirecEdit.setText(directory)
    @Slot()
    def tcompDirecClick(self):
        directory = changeDir()
        if directory != None:
            self.settings.tcompDirecEdit.setText(directory)
    @Slot()
    def backupDirecClick(self):
        directory = changeDir()
        if directory != None:
            newdirectories[3] = directory
            self.settings.backupDirecEdit.setText(directory)

    @Slot()
    def settingsCancel(self):
        self.settings.hide()
    @Slot()
    def settingsOk(self):
        HToolsDir = self.settings.HToolsDirecEdit.text()
        rectData = self.settings.dataDirecEdit.text()
        rectImages = self.settings.imageDirecEdit.text()
        tcompDir = self.settings.tcompDirecEdit.text()
        tcompBackup = self.settings.backupDirecEdit.text()
        lines = []
        with open('HTconfig.py','rt') as cfg:
            for line in cfg:
                lines.append(line)
        with open('HTconfig.py','wt',encoding = "UTF-8") as cfg:
            for line in lines:
                if "rectImages =" in line:
                    line = f'rectImages = r"{rectImages}"\n'
                elif "HToolsDir =" in line:
                    line = f'HToolsDir = r"{HToolsDir}"\n'
                elif "rectData =" in line:
                    line = f'rectData = r"{rectData}"\n'
                elif "tcompDir =" in line:
                    line = f'tcompDir = r"{tcompDir}"\n'
                elif "tcompBackup =" in line:
                    line = f'tcompBackup = r"{tcompBackup}"'
                cfg.write(line)
        reloadConfig()
        self.settings.hide()
    
    def run(self):
        self.settingsWindow()
        self.show()
        qt_app.exec_()

if __name__ == "__main__":
    global newdirectories
    app = MainWindow()
    app.run()