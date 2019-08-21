import os, shutil, zipfile, pickle, time, subprocess, TTools
from PIL import Image, ImageChops
import tkinter as tk
from tkinter import filedialog, messagebox

#Main window
window = tk.Tk()
window.title("Evo Image Summary")
window.geometry('580x100')

#Directory loading
def loading():
    global directories, directory, imdir, otcompdir, tempdir
    try:
        with open('directories.pkl','rb') as dir:
            directories = pickle.load(dir)
        directory = directories[0]
        imdir = directories[1]
        otcompdir = directories[2]
        tempdir = directories[3]
        return directories, directory, imdir, otcompdir, tempdir
    except FileNotFoundError:
        directories=['','','','']
        with open('directories.pkl','wb') as dir:
            pickle.dump(directories,dir)
loading()

#New directory change function [RectData,RectImages,Tcomp,Tcomp Template], buttons 1,2,3,4, respectively
def directorychange(button):
    if button == 1:
        with open('directories.pkl','wb') as d:
            directory = filedialog.askdirectory()
            directories[0] = directory
            pickle.dump(directories,d)

    elif button == 2:
        with open('directories.pkl','wb') as d:
            imdir = filedialog.askdirectory()
            directories[1] = imdir
            pickle.dump(directories,d)

    elif button == 3:
        with open('directories.pkl','wb') as d:
            otcompdir = filedialog.askdirectory()
            directories[2] = otcompdir
            pickle.dump(directories,d)

    elif button == 4:
        with open('directories.pkl','wb') as d:
            tempdir = filedialog.askdirectory()
            directories[3] = tempdir
            pickle.dump(directories,d)

#Radio button variable
radio=tk.StringVar()

#Global Variables
check = ''
direc = ''
folder = ''
serial = ''
foldir = ''
images_exist = None
rectdata_exist = None


#New summary button function
def newsum(starter, anaSN=None):
    loading()
    global directory, check, direc, folder, serial, foldir
    global sn
    if starter==1:
        serial="SN"+sn.get()
        if radio.get() == '1':
            folder = serial+'_b4TT/'
            check = serial+'_b4TT'
        elif radio.get() =='2':
            folder = serial+'_aftTT/'
            check = serial+'_aftTT'
        elif radio.get() =='3':
            folder = serial+'_Tcomped/'
            check = serial+'_Tcomped'
        elif radio.get() =='4':
            folder = serial+customentry.get()+'/'
            check = serial+customentry.get()
        else:
            folder = serial
            check = serial
            messagebox.showinfo('No Selection','Please select a radio button')
            return
        if serial.startswith("SN134"):
            direc = directory+"/SN134xxx/"
        elif serial.startswith("SN135"):
            direc = directory+"/SN135xxx/"
        elif serial.startswith("SN991"):
            direc = directory+"/SN991XXX"
        else:
            direc = ''
        foldir=direc+folder

        if (state0.get()==True or stateplus.get()==True or stateminus==True) and statesph.get()==True:
            dirCheck(True,True)
        elif state0.get()==True or stateplus.get()==True or stateminus==True:
            dirCheck(False, True)
        elif statesph.get()==True:
            dirCheck(True,False)

        scangrab(1)


    elif starter==2:
        serial="SN"+anaSN
        folder = serial+'/'
        check = serial

        if serial.startswith("SN134"):
            direc = directory+"/SN134xxx/"
        elif serial.startswith("SN135"):
            direc = directory+"/SN135xxx/"
        elif serial.startswith("SN991"):
            direc = directory+"/SN991XXX/"
        else:
            direc = ''
        foldir=direc+folder
         
#Verify directories
def dirCheck(rectimages,rectdata):
    global images_exist, rectdata_exist
    if rectimages == True:
        try:
            if check in os.listdir(imdir):
                images_exist = True
            else:
                images_exist = False
                messagebox.showerror('Error','Cannot find the RectImages folder for this sensor. Check the current directory and SN.')
        except FileNotFoundError:
            messagebox.showerror('Error','Cannot find the RectImages folder for this sensor. Check the current directory and SN.')
            images_exist = False

    if rectdata == True:
        try:
            if check in os.listdir(direc):
                rectdata_exist = True
            else:
                rectdata_exist = False
        except FileNotFoundError:
            messagebox.showerror('Error','Cannot find the RectData folder for this sensor. Check the current directory and SN.')
            rectdata_exist = False

    return images_exist , rectdata_exist

#Get accuracy test spheres
def scangrab(mode, a0=False,a45=False,min45=False, error='',errorcode=0, items=[]):
    if state0.get()==False and stateplus.get()==False and stateminus.get()==False and statesph.get()==False and mode==1:
        messagebox.showinfo('No Selection','Please select which images to gather.')
    if rectdata_exist == True:
        if mode==1:
            try:
                if state0.get()==True or stateplus.get()==True or stateminus.get()==True:
                    zipper = zipfile.ZipFile(foldir+serial+'.zip')
                    os.makedirs(foldir+'summary',exist_ok=True)
                    sumdir=foldir+'summary'
                    sumdir2 = sumdir.replace('/',"\\")
                    os.startfile(sumdir2)
                    if state0.get() == True:
                        for img in zipper.namelist():
                            if "AccuracySphere_A1_L0" in img and ".tif" in img:
                                zipper.extract(img,path=sumdir)
                    if stateplus.get() == True:
                        for img in zipper.namelist():
                            if "AccuracySphere_A1_L+45" in img and ".tif" in img:
                                zipper.extract(img,path=sumdir)
                    if stateminus.get() == True:
                        for img in zipper.namelist():
                            if "AccuracySphere_A1_L-45" in img and ".tif" in img:
                                zipper.extract(img,path=sumdir)
            except FileNotFoundError as e:
                if sn.get()=='':
                    messagebox.showerror('Error','Please enter a serial number.')
                else:
                    messagebox.showerror('Error','Cannot find the SNxxxxxx folder. Check the set directory and/or serial number.')
        elif mode==2:
            try:
                if errorcode != 2:
                    zipper = zipfile.ZipFile(foldir+serial+'.zip')
                os.makedirs(foldir+'Error Analysis',exist_ok=True)
                sumdir=foldir+'Error Analysis'
                sumdir2 = sumdir.replace('/',"\\")
                os.startfile(sumdir2)
                if errorcode==1:
                    if a0 == True:
                        for img in zipper.namelist():
                            if "AccuracySphere_A1_L0" in img and ".tif" in img:
                                zipper.extract(img,path=sumdir)
                        a0Details = []
                        with open(foldir+'AccDataAcq.log','rt') as log:
                            to_write = False
                            a0Details.append(error + '\n')
                            for line in log:
                                if 'Line_Zero Test Type' in line:
                                    a0Details.append(line)
                                    to_write = True
                                elif '2RMS deviation' in line:
                                    a0Details.append(line)
                                    to_write = False
                                    break
                                elif to_write == True:
                                    a0Details.append(line)
                        with open(sumdir+'/Error Details.txt','wt') as ed:
                            for line in a0Details:
                                ed.write(line)

                    if a45 == True:
                        for img in zipper.namelist():
                            if "AccuracySphere_A1_L+45" in img and ".tif" in img:
                                zipper.extract(img,path=sumdir)
                        a45Details = []
                        with open(foldir+'AccDataAcq.log','rt') as log:
                            to_write = False
                            a45Details.append(error + '\n')
                            for line in log:
                                if 'Line_Plus_45 Test Type' in line:
                                    a45Details.append(line)
                                    to_write = True
                                elif '2RMS deviation' in line:
                                    a45Details.append(line)
                                    to_write = False
                                    break
                                elif to_write == True:
                                    a45Details.append(line)
                        with open(sumdir+'/Error Details.txt','wt') as ed:
                            for line in a45Details:
                                ed.write(line)

                    if min45 == True:
                        for img in zipper.namelist():
                            if "AccuracySphere_A1_L-45" in img and ".tif" in img:
                                zipper.extract(img,path=sumdir)
                        min45Details = []
                        with open(foldir+'AccDataAcq.log','rt') as log:
                            to_write = False
                            min45Details.append(error + '\n')
                            for line in log:
                                if 'Line_Minus_45 Test Type' in line:
                                    min45Details.append(line)
                                    to_write = True
                                elif '2RMS deviation' in line:
                                    min45Details.append(line)
                                    to_write = False
                                    break
                                elif to_write == True:
                                    min45Details.append(line)
                        with open(sumdir+'/Error Details.txt','wt') as ed:
                            for line in a45Details:
                                ed.write(line)
                       
                elif errorcode==2:
                    #Not using zipfile because if this error occurs there won't be a zip folder
                    os.makedirs(sumdir + '/Rect Images')
                    for item in items:
                        for img in os.listdir(foldir):
                            if item in img:
                                shutil.copy(foldir+img, sumdir2)
                        for img in os.listdir(imdir+'/'+folder):
                            if item in img:
                                shutil.copy(imdir+'/'+folder+img,sumdir2 + '/Rect Images')
                    with open(sumdir+'/Error Details.txt','wt') as ed:
                        ed.write(error)
            except FileNotFoundError as e:
                if sn.get()=='' and mode==1:
                    messagebox.showerror('Error','Please enter a serial number.')
                else:
                    messagebox.showerror('Error','Cannot find the SNxxxxxx folder. Check the set directory and/or serial number.')
    elif images_exist == True:
        ispheresum()
    else:
        return

#Get illuminated spheres
def ispheresum():
    global images_exist
    if statesph.get()==True and images_exist==True:
        try:
            with open('{0}/{1}DataAcq.log'.format(imdir,folder),'rt') as f:
                linenum = 0
                global sphmodel
                sphmodel = []
                for line in f.readlines():
                    if linenum <= 20:
                        linenum += 1
                        if '920-0201' in line:
                            sphmodel = iter(['RZ-30000','RZ0','RZ30000','RZ60000','done'])
                            break
                        elif '920-0401' in line:
                            sphmodel = iter(['RZ-30000','RZ0','RZ30000','RZ60000','done'])
                            break
                        elif '920-0402' in line:
                            sphmodel = iter(['RZ-30000','RZ0','RZ50000','done'])
                            break
                        elif '920-0801' in line:
                            sphmodel = iter(['RZ-30000','RZ0','RZ75000','done'])
                            break
                        elif '920-1101' in line:
                            sphmodel = iter(['RZ-50000','RZ0','RZ50000','RZ100000','done'])
                            break
                        elif '920-1401' in line:
                            sphmodel = iter(['RZ-50000','RZ0','RZ50000','RZ100000','done'])
                            break
                    else:
                        messagebox.showerror('Error','Cannot identify sensor model in DataAcq.log file.')
                        break

            done = 3
            os.makedirs('{0}/{1}Summary/'.format(imdir,folder),exist_ok=True)
            distcomplete = []
            for n in range(5):
                try:
                    if done == 1:
                        break
                    else:
                        done = 0
                    for dist in sphmodel:
                        if dist in distcomplete:
                            next(sphmodel)
                        if dist=='done':
                            done = 1
                            distcomplete = []
                            path = '{0}/{1}Summary/'.format(imdir,folder)
                            netpath = path.replace('/','\\')
                            os.startfile(netpath)
                            break

                        distlist=[]
                        currentimg = 0

                        if done==1:
                            break
                        for img in os.listdir('{0}/{1}'.format(imdir,folder)):
                            if done==1:
                                break
                            if dist in img and '.tif' in img:
                                distlist.append(img)
                        for i in range(5):
                            try:
                                if dist in distcomplete:
                                    break
                                for item in distlist:
                                    currentimg = currentimg + 1
                                    if currentimg == 1:
                                        img1 = Image.open('{0}/{1}{2}'.format(imdir,folder,item))
                                        img1.save('{0}/{1}Summary/summary_{2}.tif'.format(imdir,folder,dist))
                                        img1.close()
                                    else:
                                        img1 = Image.open('{0}/{1}Summary/summary_{2}.tif'.format(imdir,folder,dist))
                                        img2 = Image.open('{0}/{1}{2}'.format(imdir,folder,item))
                                        summaryimg = ImageChops.lighter(img1,img2)
                                        img1.close()
                                        img2.close()
                                        os.unlink('{0}/{1}Summary/summary_{2}.tif'.format(imdir,folder,dist))
                                        summaryimg.save('{0}/{1}Summary/summary_{2}.tif'.format(imdir,folder,dist))
                                        summaryimg.close()
                                        if item == distlist[-1]:
                                            distcomplete.append(dist)
                                            break
                            except PermissionError:
                                if i==4:
                                    for file in os.listdir('{0}/{1}Summary/'.format(imdir,folder)):
                                        if 'RZ' in file:
                                            os.unlink('{0}/{1}Summary/{2}'.format(imdir,folder,file))
                                    messagebox.showerror('Error','Access is denied.')


                except PermissionError as r:
                    if i==4:
                        for file in os.listdir('{0}/{1}Summary/'.format(imdir,folder)):
                            if 'RZ' in file:
                                os.unlink('{0}/{1}Summary/{2}'.format(imdir,folder,file))
                        messagebox.showerror('Error','Access is denied.')

        except FileNotFoundError as e:
            print(e)
            messagebox.showerror('Error','Cannot find "DataAcq.log" file in rect images folder.')

#Function to launch child windows
def windowlauncher(select):
    if select == 1:
        root=tk.Toplevel(master=window)
        template(root)
    else:
        root=tk.Toplevel(master=window)
        analyzer(root)

#Inject Tcomp Template
class template:
    def __init__(self, master):
        self.master = master
        master.title("Tcomp Inject")
        master.geometry('350x200')
        rows = 0
        while rows < 50:
            master.rowconfigure(rows, weight=1)
            master.columnconfigure(rows,weight=1)
            rows += 1
        self.ok_button = tk.Button(master, text='Inject', command=self.okbtn)
        self.ok_button.grid(column=2,row=40)
        self.cancel_button = tk.Button(master, text="Cancel", command=master.destroy)
        self.cancel_button.grid(column=3,row=40)
        global model
        model = tk.StringVar()
        self.x0200 = tk.Radiobutton(master, text='X0200',value='X0200',variable=model)
        self.x0200.grid(column=0,row=5)
        self.x0400 = tk.Radiobutton(master, text='X0400',value='X0400',variable=model)
        self.x0400.grid(column=1,row=5)
        self.x0800 = tk.Radiobutton(master, text='X0800',value='X0800',variable=model)
        self.x0800.grid(column=2,row=5)
        self.x1100 = tk.Radiobutton(master, text='X1100',value='X1100',variable=model)
        self.x1100.grid(column=3,row=5)
        self.x1400 = tk.Radiobutton(master, text='X1400',value='X1400',variable=model)
        self.x1400.grid(column=4,row=5)
        #Serial Number entry
        self.tcompsn = tk.Entry(master, width=15)
        self.tcompsn.grid(column=1,row=10,sticky=tk.W,columnspan=2)
        self.tsnlbl = tk.Label(master, text="SN:")
        self.tsnlbl.grid(column=0,row=10)
        #Restore Tcomp button
        self.restore = tk.Button(master, text='Restore Original Tcomp',command=self.restorebtn)
        self.restore.grid(column=2,row=30,columnspan=2)

    def restorebtn(self):
        loading()
        if self.tcompsn.get().startswith('134'):
            tcompdir = otcompdir+"/SN134XXX"
        elif self.tcompsn.get().startswith('135'):
            tcompdir = otcompdir+"/SN135XXX"
        elif self.tcompsn.get().startswith('991'):
            tcompdir = otcompdir+"/991XXX"
        else:
            messagebox.showerror('Error','Cannot find the tcomp directory for this SN, check the serial number and/or directory.')
            return None
        try:
            ofile="SN"+self.tcompsn.get()+".txt"
            shutil.move(tempdir+'/Originals/'+ofile,tcompdir+'/'+ofile)
            messagebox.showinfo('Done','Original Tcomp file restored.')
        except FileNotFoundError:
            try:
                ofile2="SN"+self.tcompsn.get()+".day2"
                shutil.move(tempdir+'/Originals/'+ofile2,tcompdir+'/'+ofile2)
                os.unlink(tcompdir+'/'+ofile)
                messagebox.showinfo('Done','Original Tcomp file restored.')
            except FileNotFoundError:
                messagebox.showerror('Error','Cannot find the original Tcomp file.')
    def okbtn(self):
        loading()
        tfile = "SN"+self.tcompsn.get()+".txt"
        if self.tcompsn.get().startswith('134'):
            tcompdir = otcompdir+"/SN134XXX"
        elif self.tcompsn.get().startswith('135'):
            tcompdir = otcompdir+"/SN135XXX"
        elif self.tcompsn.get().startswith('991'):
            tcompdir = otcompdir+"/991XXX"
        else:
            messagebox.showerror('Error','Cannot find the tcomp directory for this SN, check the serial number and/or directory.')
            return None
        try:
            shutil.move(tcompdir+"/"+tfile,tempdir+'/Originals')
            try:
                shutil.copy(tempdir+'/'+model.get()+' Tcomp Template.txt',tcompdir)
                os.rename(tcompdir+'/'+model.get()+' Tcomp Template.txt',tcompdir+'/'+tfile)
                messagebox.showinfo('Done','Tcomp injected successfully.')
            except FileNotFoundError:
                messagebox.showerror('Error','Unable to find Tcomp Template for '+model.get()+'.')
                shutil.move(tempdir+'/Originals/'+tfile,tcompdir+'/'+tfile)
        except shutil.Error:
            if messagebox.askyesno('File Found','There is a Tcomp file for this sensor in the originals folder. Do you want to replace it?'):
                os.unlink(tempdir+'/Originals/'+tfile)
                self.okbtn()
        except FileNotFoundError:
            tfile2 = "SN"+self.tcompsn.get()+".day2"
            try:
                shutil.move(tcompdir+"/"+tfile2,tempdir+'/Originals')
                try:
                    shutil.copy(tempdir+'/'+model.get()+' Tcomp Template.txt',tcompdir)
                    os.rename(tcompdir+'/'+model.get()+' Tcomp Template.txt',tcompdir+'/'+tfile)
                    messagebox.showinfo('Done','Tcomp injected successfully.')
                except FileNotFoundError:
                    messagebox.showerror('Error','Unable to find Tcomp Template for '+model.get()+'.')
                    shutil.move(tempdir+'/Originals/'+tfile2,tcompdir+'/'+tfile2)
            except FileNotFoundError:
                    messagebox.showerror('Error','Original Tcomp file not found.')

#Error Analyzer
class analyzer:
    def __init__(self,master):
        self.master = master
        master.title("Error Analyzer")
        master.geometry('450x100')
        rows = 0
        while rows < 50:
            master.rowconfigure(rows, weight=1)
            master.columnconfigure(rows,weight=1)
            rows += 1
        self.snLabel = tk.Label(master,text='SN: ')
        self.snLabel.grid(column=1,row=1, sticky=tk.E)
        self.errorLabel = tk.Label(master,text='Error Message: ')
        self.errorLabel.grid(column=1,row=2, sticky=tk.E)
        self.snEntry = tk.Entry(master, width=15)
        self.snEntry.grid(column=2,row=1,sticky=tk.W)
        self.errorEntry = tk.Entry(master,width=45)
        self.errorEntry.grid(column=2,row=2,sticky=tk.W,columnspan=3)
        self.startBtn=tk.Button(master,text='Start',command=self.start)
        self.startBtn.grid(column=3,row=4)
    def start(self):
        anaSN = self.snEntry.get()
        if '2RMS' in self.errorEntry.get() or 'Max Deviation' in self.errorEntry.get():
            if '0 Degree' in self.errorEntry.get():
                self.a0 = True
                self.a45 = False
                self.min45 = False
            elif '+45 Degree' in self.errorEntry.get():
                self.a0 = False
                self.a45 = True
                self.min45 = False
            elif '-45 Degree' in self.errorEntry.get():
                self.a0 = False
                self.a45 = False
                self.min45 = True
            self.error = self.errorEntry.get()
            newsum(2, anaSN)
            dirCheck(False, True)
            scangrab(2,self.a0,self.a45,self.min45,self.error,errorcode=1)
        elif 'Excessive Linearity' in self.errorEntry.get():
            item = ''
            images_item = ''
            items = []
            startitem = False
            for char in self.errorEntry.get():
                if char == '[':
                    startitem = True
                elif startitem == True and char != ']' and char != 'Z' and char != '.':
                    item = item + char
                    images_item = images_item + char
                elif char == 'Z' and startitem == True:
                    item = item + 'L'
                    images_item = images_item + char
                elif char == '.' and startitem == True:
                    item = item + char
                elif char == ']':
                    startitem = False
                    break
            if 'A0' in images_item:
                images_item = images_item[2:] + images_item[:2] 
            elif 'A45' in images_item:
                images_item = images_item[3:] + images_item[:3]
            elif 'A-45' in images_item:
                images_item = images_item[4:] + images_item[:4]
            images_item = 'T' + images_item
            items.append(item)
            items.append(images_item)
            newsum(2, anaSN)
            dirCheck(True,True)
            scangrab(2,error = self.errorEntry.get(),errorcode=2, items = items)
        elif 'points removed' in self.errorEntry.get():
            files = []
            for file in os.listdir('{0}\\SN{1}'.format(imdir,anaSN)):
            #for file in os.listdir('\\\\castor\\Production\\Manufacturing\\Evo\\ALS Point Removal Testing'):
                if 'CGs.txt' in file:
                    files.append('{0}\\SN{1}\\{2}'.format(imdir,anaSN,file))
                    #files.append('\\\\castor\\Production\\Manufacturing\\Evo\\ALS Point Removal Testing\\{}'.format(file))
            pointremover(files)
            messagebox.showinfo('Done','Points removed.')

#Menu Bar
menu = tk.Menu(window)
new_item = tk.Menu(menu)
new_item.add_command(label='Set Tcomp Directory',command=lambda: directorychange(3))
new_item.add_command(label='Set Template Directory',command=lambda: directorychange(4))
new_item.add_command(label='Set Rect Images Directory',command=lambda: directorychange(2))
menu.add_cascade(label='File', menu=new_item)
window.config(menu=menu)

#Top Row Buttons
#New summary button
newbtn = tk.Button(window, text="New Summary",command=lambda: newsum(1))
newbtn.grid(column=1,row=0)
#Set directory button
direcbutton = tk.Button(window,text='RectData Directory',command=lambda: directorychange(1))
direcbutton.grid(column=2,row=0)
#Tcomp Template button
tcompbutton = tk.Button(window,text='Inject Tcomp Template',command=lambda: windowlauncher(1))
tcompbutton.grid(column=3,row=0)
#Error Analyzer button
eAnalyze = tk.Button(window, text='Analyze Error', command=lambda: windowlauncher(2))
eAnalyze.grid(column=4,row=0)

#The B4TT, AftTT, Tcomped radio buttons
def customclick():
    global customentry
    customentry = tk.Entry(window,width=10)
    customentry.grid(column=5,row=2,sticky=tk.W)
def otherclick():
    try:
        customentry.grid_remove()
    except:
        return

b4tt = tk.Radiobutton(window,text='B4TT',value=1,variable=radio,command=otherclick)
afttt = tk.Radiobutton(window,text='AftTT',value=2,variable=radio,command=otherclick)
tcomped = tk.Radiobutton(window,text='Tcomped',value=3,variable=radio,command=otherclick)
custom = tk.Radiobutton(window,text='Custom',value=4,variable=radio,command=customclick)
b4tt.grid(column=1,row=2)
afttt.grid(column=2,row=2)
tcomped.grid(column=3,row=2)
custom.grid(column=4,row=2)

#image selection checkboxes
state0 = tk.BooleanVar()
state0.set(False)
stateplus = tk.BooleanVar()
stateplus.set(False)
stateminus = tk.BooleanVar()
stateminus.set(False)
statesph = tk.BooleanVar()
statesph.set(False)

zerodegree = tk.Checkbutton(window,text="0 Degree Sphere", var=state0)
zerodegree.grid(column=1,row=3)
plus45 = tk.Checkbutton(window, text="+45 Degree Sphere", var=stateplus)
plus45.grid(column=2,row=3)
minus45 = tk.Checkbutton(window, text="-45 Degree Sphere", var=stateminus)
minus45.grid(column=3,row=3)
ispheres = tk.Checkbutton(window,text="Illuminated Spheres",var=statesph)
ispheres.grid(column=4,row=3)

#Serial number entry
sn = tk.Entry(window, width=15)
sn.grid(column=2,row=4,sticky=tk.W)
snlbl = tk.Label(window, text="Serial Number")
snlbl.grid(column=1,row=4)

#Mainloop
window.mainloop()

