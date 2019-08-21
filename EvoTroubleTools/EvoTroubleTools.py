import os, shutil, zipfile, pickle, time, subprocess, sys, TTools
from PIL import Image, ImageChops
import tkinter as tk
from tkinter import filedialog, messagebox
from PySide2.QtCore import *
from PySide2.QtGui import *
from PySide2.QtWidgets import *

testdirectory = 'C:\\Users\\grobinson\\Documents\\testdump\\Icon testing\\'\

#Directory loading
def loading():
    global directories, directory, imdir, otcompdir, backupdir
    try:
        with open('directories.pkl','rb') as dir:
            directories = pickle.load(dir)
        directory = directories[0]
        imdir = directories[1]
        otcompdir = directories[2]
        backupdir = directories[3]
        return directories, directory, imdir, otcompdir, backupdir
    except FileNotFoundError:
        directories=['','','','']
        with open('directories.pkl','wb') as dir:
            pickle.dump(directories,dir)

def save():
    global calitemslist
    with open('directories.pkl','wb') as dirs:
        pickle.dump(calitemslist, dirs)
    return

def changeDir():
    global directories, directory, imdir, otcompdir, backupdir
    root = tk.Tk()
    root.withdraw()
    direc = filedialog.askdirectory()
    directory = direc.replace('/','\\') + '\\'
    if directory != '':
        return directory
    root.destroy()
    loading()
    return

qt_app = QApplication(sys.argv)

class MainWindow(QMainWindow):
    def __init__(self):
        #Initialize the parent class, then set title and min window size
        QMainWindow.__init__(self)
        self.setWindowTitle('Evo Trouble Tools v0.2.0')
        self.setMinimumSize(300,400)

        #Set QWidget as central for the QMainWindow, QWidget will hold layouts
        self.centralwidget = QWidget()
        self.setCentralWidget(self.centralwidget)

        #Create the layouts
        self.layout = QGridLayout()
        #Menu Bar
        self.menubar = self.menuBar()

        exitaction = QAction("Exit",self)
        filemenu = self.menubar.addMenu("&File")
        filemenu.addAction(exitaction)

        #Create the top row buttons to go into the sublayout (which is then in the main layout)
        self.algoBtn = QPushButton('Fix TComp Algorithm Errors (SNXXXXXX)')
        self.algoBtn.clicked.connect(self.tcompBtnClick)
        self.restoreBtn = QPushButton('Restore original TComp (SNXXXXXX)')
        self.restoreBtn.clicked.connect(self.restoreBtnClick)
        self.alsPointBtn = QPushButton('Fix ALS Points (Directory)')
        self.alsPointBtn.clicked.connect(self.alsBtnClick)
        self.dotBtn = QPushButton('Remove Staring Dots (Directory)')
        self.dotBtn.clicked.connect(self.dotBtnClick)
        self.settingsBtn = QPushButton('Settings')

        self.settingsBtn.clicked.connect(self.settingsClick)

        self.summaryBtn = QPushButton('Illuminated sphere summary (Directory)')
        self.summaryBtn.clicked.connect(self.summaryClick)
        self.directoryEdit = QLineEdit(self)
        #self.errorMessageEdit = QLineEdit(self)

        self.directoryEdit.setPlaceholderText('Directory/SN')
        #self.errorMessageEdit.setPlaceholderText('Error Message (Not Functional)')

        self.layout.addWidget(self.settingsBtn,0,0)
        self.layout.addWidget(self.algoBtn,1,0)
        self.layout.addWidget(self.restoreBtn,2,0)
        self.layout.addWidget(self.alsPointBtn,3,0)
        self.layout.addWidget(self.dotBtn,4,0)
        self.layout.addWidget(self.summaryBtn,5,0)
        self.layout.addWidget(self.directoryEdit,6,0)
        #self.layout.addWidget(self.errorMessageEdit,7,0)
        self.layout.setColumnMinimumWidth(0,256)

        #self.layout.setColumnStretch(1,1)
        #self.layout.setColumnStretch(2,1)
        #Add the sublayouts to the main layout

        self.centralwidget.setLayout(self.layout)
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

    #Slots for buttons and main functions---------------------
    @Slot()
    def summaryClick(self):
        TTools.summary(self.directoryEdit.text())

    @Slot()
    def alsBtnClick(self):
        TTools.pointRemover(directory = self.directoryEdit.text())

    @Slot()
    def dotBtnClick(self):
        TTools.dotEraser(self.directoryEdit.text())

    @Slot()
    def tcompBtnClick(self):
        TTools.fixAlgoErrors(self.directoryEdit.text(),otcompdir,backupdir)

    @Slot()
    def restoreBtnClick(self):
        TTools.restoreTcomp(sn = self.directoryEdit.text(),tcompDir = otcompdir,backupDir = backupdir)

    @Slot()
    def settingsClick(self):
        #Display current set directories
        self.settings.dataDirecEdit.setText(directories[0])
        self.settings.imageDirecEdit.setText(directories[1])
        self.settings.tcompDirecEdit.setText(directories[2])
        self.settings.backupDirecEdit.setText(directories[3])

        self.settings.show()

    #Settings button slots
    @Slot()
    def dataDirecClick(self):
        directory = changeDir()
        if directory != None:
            newdirectories[0] = directory
            self.settings.dataDirecEdit.setText(directory)
    @Slot()
    def imageDirecClick(self):
        directory = changeDir()
        if directory != None:
            newdirectories[1] = directory     
            self.settings.imageDirecEdit.setText(directory)
    @Slot()
    def tcompDirecClick(self):
        directory = changeDir()
        if directory != None:
            newdirectories[2] = directory
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
        directories[0] = self.settings.dataDirecEdit.text()
        directories[1] = self.settings.imageDirecEdit.text()
        directories[2] = self.settings.tcompDirecEdit.text()
        directories[3] = self.settings.backupDirecEdit.text()
        with open('directories.pkl','wb') as d:
                pickle.dump(directories,d)
        loading()
        self.settings.hide()
    
    def run(self):
        self.settingsWindow()
        self.show()
        qt_app.exec_()

if __name__ == "__main__":
    global newdirectories
    loading()
    newdirectories = directories
    app = MainWindow()
    app.run()