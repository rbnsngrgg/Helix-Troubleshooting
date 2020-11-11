import os, shutil, math, HTconfig
import xml.etree.ElementTree as ET
import ImageClasses as IC
from PySide2.QtWidgets import QMessageBox, QProgressDialog
from PySide2.QtCore import Qt
from PIL import Image,ImageChops,ImageDraw
from statistics import mean

def pointRemover(directory = '', sn = ''):
    try:
        files = []
        #Find ALS point files in directory
        for file in os.listdir(directory):
            if "CGs.txt" in file:
                files.append(directory + '\\' + file)
        if files == []:
            raise FileNotFoundError("No CG text files were found.")

        for file in files:
            #rowNums will be in dict in format: {number: occurences}
            rowNums = {}
            with open(file,'rt') as f:
                #Find number of occurences for each "R" column integer, and add it to rowNums using above format.
                data = f.readlines()
                lineNum = 0
                for line in data:
                    lineNum += 1
                    if lineNum == 1:
                        continue
                    lineSplit = line.split()
                    rSplit = lineSplit[5].split('.')

                    if int(rSplit[0]) in rowNums.keys():
                        rowNums[int(rSplit[0])] += 1
                    else:
                        try:
                            rowNums[int(rSplit[0])] = 1
                        except:
                            pass

            #Use rowNums dict to determine numbers that have greater than 5 occurences, then remove all lines with those numbers in R column.
            toRemove = []
            for row in rowNums.keys():
                if rowNums[row] > 3:
                    toRemove.append(row)
            newText = []
            for line in data:
                keepLine = True
                for number in toRemove:
                    if str(number) in line.split()[5]:
                        keepLine = False
                if keepLine == True:
                    newText.append(line)
            #Rewrite the file with selected lines removed.
            with open(file,'wt') as f:
                f.writelines(newText)

        successMessage("Done","ALS Points cleaned.")

    except Exception as e:
        errorMessage('Error!', str(e))


def dotEraser(directory):
    try:
        files = []
        zValues = []
        #Find Line image files in directory
        fileNames = os.listdir(directory)
        #Get list of all relevant files, and list of all zValues
        for file in fileNames:
            if "TZ" in file and ".tif" in file and "UVRC" not in file:
                zValue = file.split('A')[0].replace('TZ','')
                if zValue not in zValues:
                    zValues.append(zValue)
                files.append(directory + '\\' + file)
        if files == []:
            raise FileNotFoundError("No TZ image files were found.")
        #Searchbox is tuple with pixel coords, where within the algorithm will search for staring dot.
        searchBox = ()
        for z in zValues:
            file1 = ''
            file2 = ''
            #Find two files to perform logical AND operation
            for file in files:
                if z in file and 'A0' in file:
                    if file1 == '':
                        file1 = file
                    elif file2 == '':
                        file2 = file
                    elif file1 != '' and file2 != '':
                        break
            image1 = Image.open(file1)
            image1 = image1.convert(mode = "1")
            image2 = Image.open(file2)
            andImage = ImageChops.darker(image1,image2)

            px = andImage.load()
            minX = 1917
            maxX = 0
            minY = 660
            maxY = 510
            for y in range (510,660):
                for x in range(0,1917):
                    value = px[x,y]
                    if value != 0:
                        if x < minX:
                            minX = x
                        if x > maxX:
                            maxX = x
                        if y < minY:
                            minY = y
                        if y > maxY:
                            maxY = y
            if minX == 1917:
                continue
            image1.close()
            image2.close()
            andImage.close()
            #Perform ops on all files in current z
            for file in files:
                if z in file:
                    image = Image.open(file)
                    newImage = image.copy()
                    draw = ImageDraw.Draw(newImage)
                    draw.rectangle(xy = [(minX-2,minY-2),(maxX+2,maxY+2)], fill = 0)
                    filename = file.split('\\')[-1]
                    image.close()
                    newImage.save(file)
                    newImage.close()
        successMessage('Done','All staring dots removed.')
    except Exception as e:
        errorMessage("Error!", str(e))


def summary(directory):
    try:
        zValues = []
        files = []
        fileNames = os.listdir(directory)
        #Find all RZ files and get a list of all zValues
        for file in fileNames:
            if "RZ" in file and ".tif" in file:
                zValue = file.split('Y')[0].replace('RZ','')
                if zValue not in zValues:
                    zValues.append(zValue)
                files.append(directory + '\\' + file)
        if files == []:
            raise FileNotFoundError("No RZ image files were found.")
        summaryFolder = directory + '\\summary'
        os.mkdir(summaryFolder)
        #For each zValue, merge all RZ images for that zValue into one image, then place it into the summary folder
        for zValue in zValues:
            summaryImg = Image.new(mode = 'L', size = (1920,1200))
            fileName = 'Z{0}_summary.tif'.format(zValue)
            for file in files:
                if zValue in file:
                    image = Image.open(file)
                    summaryImg = ImageChops.lighter(summaryImg,image)
                    image.close()
            summaryImg.save(summaryFolder + '\\' + fileName)
            summaryImg.close()
        successMessage('Done','Summary images created!')
    except Exception as e:
        errorMessage('Error!',str(e))


def fixAlgoErrors(sn = '',tcompDir = '',backupDir = ''):
    try:
        #Find folder and file
        selectedFile, selectedFolder = getFileFolder(sn,tcompDir)
        if selectedFile == '' or selectedFolder == '':
            return

        #Copy file to backup folder
        shutil.copy(selectedFile,backupDir)

        #Read the tcomp file
        with open(selectedFile,'rt') as f:
            data = f.readlines()
        #Find values that are algo error output and replace them.
        splitData = []
        for line in data:
            splitData.append(line.split('\t'))
        newData = []
        lineNum = 0
        firstLineDone = False
        for line in splitData:
            newLine = []
            if line[0] == 'unit' and firstLineDone == False:
                newLine = line
                newData.append('\t'.join(newLine))
                firstLineDone = True
                lineNum += 1
                continue
            for index,value in enumerate(line):
                newValue = ''
                if ':' in value:
                    newValue = value
                    newLine.append(newValue)
                    continue
                try:
                    #Check previous and next values for algo errors, then check an additional +- 1 if they are also algo errors.
                    if float(value) > 500.0 and len(splitData) -2 > lineNum > 1:
                        previous = float(splitData[lineNum - 1][index])
                        previousPlus = float(splitData[lineNum - 2][index])
                        next = float(splitData[lineNum + 1][index])
                        nextPlus = float(splitData[lineNum + 2][index])
                        if previous < 500.0 and next < 500.0:
                            newValue = str((previous + next) / 2)
                            #print("Value on line {0}, index {1} is set to {2}, average of {3} and {4}".format(lineNum + 1, index, newValue, previous, next))
                        elif previousPlus < 500.0 and nextPlus < 500.0:
                            newValue = str((previousPlus + nextPlus) / 2)
                            #print("Value on line {0}, index {1} is set to {2}, average of (+) {3} and {4}".format(lineNum + 1, index, newValue, previousPlus, nextPlus))
                        else:
                            raise Exception('Too many consecutive algo errors. Manual fix or template required. Line {0}'.format(lineNum))

                    #Case for if the algo error is on the first line of data
                    elif float(value) > 500.0 and lineNum == 1:
                        next = float(splitData[lineNum + 1][index])
                        nextPlus = float(splitData[lineNum + 2][index])
                        if next < 500.0:
                            newValue = str(next)
                        elif nextPlus < 500.0:
                            newValue = str(nextPlus)
                        else:
                            raise Exception('Too many consecutive algo errors. Manual fix or template required. Line {0}'.format(lineNum + 1))

                    #Case for if the algo error is on the last line of data
                    elif float(value) > 500.0 and lineNum == len(splitData) - 1:
                        previous = float(splitData[lineNum - 1][index])
                        previousPlus = float(splitData[lineNum - 2][index])
                        if previous < 500.0:
                            newValue = str(previous)
                        elif previousPlus < 500.0:
                            newValue = str(previousPlus)
                        else:
                            raise Exception('Too many consecutive algo errors. Manual fix or template required. Line {0}'.format(lineNum + 1))
                    elif float(value) < 500.0:
                        newValue = value

                    newLine.append(newValue)
                except ValueError:
                    pass
            if '\n' not in newData[-1]:
                newData[-1] = newData[-1] + '\n'
            newData.append('\t'.join(newLine))

            lineNum += 1
        if '.Day2' in selectedFile:
            txtSelectedFile = selectedFile.replace('.Day2','.txt')
        else:
            txtSelectedFile = selectedFile
        with open(txtSelectedFile,'wt') as f:
            f.writelines(newData)
        if selectedFile != txtSelectedFile:
            os.unlink(selectedFile)
        successMessage('Done', 'Algorithm errors fixed.')
    except Exception as e:
        errorMessage('Error!', str(e))


def restoreTcomp(sn = '', tcompDir = '', backupDir = ''):
    try:
        if sn == '':
            raise Exception('No serial number was entered.')
        elif 'SN' not in sn or len(sn) < 8:
            raise Exception('Serial number must be in SNXXXXXX format.')
        originalFile = ''
        fileToRemove = ''
        snFolder = ''
        removeFile = True
        #Find original Tcomp file
        for file in os.listdir(backupDir):
            if sn in file:
                originalFile = backupDir + '\\' + file
                break
        if originalFile == '':
            raise Exception('Cannot find tcomp file for ' + sn + ' in backup folder.')

        #Find correct folder in tcomp directory
        for folder in os.listdir(tcompDir):
            if sn[:5] in folder:
                snFolder = tcompDir + '\\' + folder
                break
        if snFolder == '':
            raise Exception('Cannot find Thermal Tester results folder for {0} sensors.'.format(sn[:5]))

        #Find tcomp file to be replaced by the original
        for file in os.listdir(snFolder):
            if sn in file:
                fileToRemove = snFolder + '\\' + file
                break
        if fileToRemove == '':
            removeFile = False
    except Exception as e:
        errorMessage('Error!',str(e))
        return

    try:
        if removeFile == True:
            os.unlink(fileToRemove)
    except Exception as e:
        errorMessage('Error!', 'Error removing Tcomp file: ' + str(e))
        return
    try:
        shutil.copy(originalFile,snFolder)
    except Exception as e:
        errorMessage('Error!', 'Error restoring original Tcomp file: ' + str(e))
        return
    try:
        os.unlink(originalFile)
        successMessage('Done','Original TComp file restored')
    except Exception as e:
        errorMessage('Error!', 'Error removing Tcomp file from backup folder: ' + str(e))
        return

def tempAdjust(sn = '',setTemp = None, tcompDir = '', backupDir = ''):
    try:
        if setTemp == None or setTemp == '':
            errorMessage("Error","No set temperature was entered.")
            return
        #Find folder and file
        selectedFile, selectedFolder = getFileFolder(sn,tcompDir)
        if selectedFile == '' or selectedFolder == '':
            return
        shutil.copy(selectedFile,backupDir)

        with open(selectedFile,'rt') as f:
            data = f.readlines()
        #Read tempurature values and get reference average
        splitData = []
        for line in data:
            splitData.append(line.split('\t'))
        tempuratures = []
        for line in splitData:
            tempuratures.append(line[1])
        reference = tempuratures[1:6]
        floatRef = []
        for temp in reference:
            floatRef.append(float(temp))
        referenceAverage = sum(floatRef) / len(floatRef)
        #print(f"Reference Average: {referenceAverage}")
        adjustBy = float(setTemp) - referenceAverage
        #print(f"Adjusting {adjustBy} degrees.")
        targetAverage = referenceAverage + adjustBy
        #print(f"Setting new reference average to {targetAverage}")

        newData = splitData
        newReference = []
        for index, line in enumerate(newData):
            if index == 0:
                continue
            line[1] = str(float(line[1]) + adjustBy)
            if index < 6:
                newReference.append(float(line[1]))
        newRefAvg = sum(newReference) / len(newReference)
        #print(f"New reference average set to {newRefAvg}")
        writeData = []
        for line in newData:
            writeData.append('\t'.join(line))

        with open(selectedFile,'wt') as f:
            f.writelines(writeData)
        successMessage("Done",f"Reference temperature for {sn} adjusted to {setTemp}")
    except Exception as e:
        errorMessage("Error",str(e))

def lineAnalysis(directory, thresholdPercent, all ,masterLog):
    if not all:
        folders = [directory]
    else:
        folders = []
        for folder in os.listdir(directory):
            folders.append(f"{directory}\\{folder}")
        pd = QProgressDialog("Analyzing all images. This may take several minutes..","Cancel",0,len(folders))
        pd.setAutoClose(True)
        pd.setFixedSize(300,120)
        pd.setWindowTitle("Line Analysis")
        pd.setWindowModality(Qt.WindowModal)
        progress = 0
    for folder in folders:
        if not os.path.isdir(folder):
            progress += 1
            pd.setValue(progress)
            continue
        masterEntry = ''
        images = []
        zValues = []
        angles = []
        masterAngle = None
        minMaxWidth = [99,0]
        focusNSF = [0,0,0]
        dataFolder = folder+'\\line analysis'

        #Get Data from sensor.xml (SNXXXXXX.xml) for master log file entry
        sn, pn, sensorRev, rectRev, rectPosRev, date, rows, cols = getSensorData(folder)
        lineExposure = getLineExposure(folder)
        masterEntry = f"\n{sn}\t{pn}\t{date}\t{sensorRev}\t{rectRev}\t{rectPosRev}\t"
        if '916-0401' in pn:
            rotated = True
        else:
            rotated = False
        #Find all images, make list in of LaserImage class instances, create results folder. Raise error if none found---------------------
        images, zValues = imageList(folder,'solo', rotate = rotated)

        if images == [] and not all:
            errorMessage('Error!','No non-filtered TZ image files were found.')
            return
        elif '916' not in pn and '917' not in pn and not all:
            if pn == '':
                errorMessage('Error!','The selected sensor is not a Helix Solo or V7 sensor.')
                return
        elif all and ('916' in pn or '917' in pn) and images != []:
            progress += 1
            pd.setValue(progress)
            continue
        if not os.path.isdir(dataFolder):
            os.mkdir(dataFolder)
        if not all:
            pd = QProgressDialog("Analyzing images.","Cancel",0,len(images))
            pd.setAutoClose(True)
            pd.setFixedSize(300,120)
            pd.setWindowTitle("Line Analysis")
            pd.setWindowModality(Qt.WindowModal)
            progress = 0
        if pd.wasCanceled():
            break
        with open(f'{dataFolder}\\Focus Summary.txt','wt') as focusLog:
            focusLog.write(f'{sn}\t{pn}\t{date}\n{lineExposure}\nZ-Values\tFocus Score\n')
        zValues.sort()
        #Go through z values in ascending order, get matching img. For each img, create data text file and perform ops-------------------------
        for z in zValues:
            if pd.wasCanceled():
                break
            for lineImage in images:
                if f'TZ{str(z)}Y' not in lineImage.name:
                    continue
                #Start log entry
                with open('{}\\{}.txt'.format(dataFolder,lineImage.name),'wt') as log:
                    log.write(f'{sn}\t{pn}\t{date}\n{lineImage.name}\n')
                    if rotated:
                        log.write('CG_Col\tCG_Row\tWidth\tPeak_Intensity\tFocus_Score\tCol-Value_Pixel_Data\n')
                    else:
                        log.write('CG_Row\tCG_Col\tWidth\tPeak_Intensity\tFocus_Score\tRow-Value_Pixel_Data\n')
                lineImage.rows = int(rows)
                lineImage.columns = int(cols)
                lineImage.thresholdPercent = thresholdPercent
                if lineImage.rotated:
                    lineImage.startAnalysis90()
                else:
                    lineImage.startAnalysis()
                if lineImage.lineAngle != None:
                    angles.append(lineImage.lineAngle)
                #Focus Data
                with open(f'{dataFolder}\\Focus Summary.txt','at') as focusLog:
                    focusLog.write(f'{lineImage.zValue}\t{lineImage.overallFocus}\n')
                #Prep data for master log entry
                if lineImage.widthAvg < minMaxWidth[0]:
                    minMaxWidth[0] = lineImage.widthAvg
                if lineImage.widthAvg > minMaxWidth[1]:
                    minMaxWidth[1] = lineImage.widthAvg
                if z == zValues[0]:
                    focusNSF[0] = lineImage.overallFocus
                elif z == zValues[-1]:
                    focusNSF[2] = lineImage.overallFocus
                elif int(z) == 0:
                    focusNSF[1] = lineImage.overallFocus
                with open('{}\\{}.txt'.format(dataFolder,lineImage.name),'at') as log:
                    for line in lineImage.logLines:
                        log.write(line)
                if not all:
                    progress += 1
                    pd.setValue(progress)
        if len(angles) > 0:
            masterAngle = round(mean(angles),2)
        masterEntry = masterEntry + f"{minMaxWidth[0]}\t{minMaxWidth[1]}\t{focusNSF[0]}\t{focusNSF[1]}\t{focusNSF[2]}\t{masterAngle}\t{thresholdPercent}"
        if not pd.wasCanceled():
            with open(masterLog,'at') as mLog:
                mLog.write(masterEntry)
        if not all:
            if pd.wasCanceled():
                successMessage('Canceled','User canceled.')
            else:
                successMessage('Done','Images analyzed.')
        if all:
            progress += 1
            pd.setValue(progress)
    if all:
        if pd.wasCanceled():
            successMessage('Canceled','User canceled.')
        else:
            successMessage('Done','All Solo laser images analyzed.')

#Gathers the filenames of all of the images required for analysis
def imageList(directory,mode = 'solo', rotate = False):
    images = []
    zValues = []
    if mode == 'solo':
        for img in os.listdir(directory):
            if 'TZ' in img and 'Filter' not in img and '.tif' in img:
                z = getZ(img)
                try:
                    zValues.append(int(z))
                except Exception:
                    continue
                newImage = IC.LaserImage()
                newImage.name = img.replace('.tif','')
                newImage.location = directory+'\\'+img
                newImage.zValue = z
                if rotate:
                    newImage.rotated = True
                images.append(newImage)
        return images, zValues

def getZ(text):
    #Format TZ-0Y0X0.tif
    yIndex = 0
    for index,char in enumerate(text):
        if char == 'Y':
            yIndex=index
            break
    text = text.replace(text[yIndex:],'')
    text = text.replace('TZ','')
    return text

#Handling XML Data-------------------------------------------------------------------------------------------------------------------------
def getSensorData(directory):
    try:
        sensorXML = ''
        for file in os.listdir(directory):
            if 'SN' in file and '.xml' in file and (len(file) < 16):
                sensorXML = f"{directory}\\{file}"
        tree = ET.parse(sensorXML)
        root = tree.getroot()
        sn = root[0][0].attrib['Sensor_ID']
        pn = root[0][0].attrib['Part_Number']
        sensorRev = root[0][0].attrib['Part_Rev']
        rectRev = root[0][0].attrib['Rect_File_Rev']
        rectPosRev = root[0][0].attrib['Rect_Pos_File_Rev']
        date = root.attrib['Date']
        rows = root[1][0].attrib['Number_Of_Rows']
        cols = root[1][0].attrib['Number_Of_Columns']
    except Exception as e:
        errorMessage('Error!',f"TTools.py getSensorData: {str(e)}")
        tree = ''
        root = ''
        sn = ''
        pn = ''
        sensorRev = ''
        rectRev = ''
        rectPosRev = ''
        date = ''
        rows = 0
        cols = 0
    return sn, pn, sensorRev, rectRev, rectPosRev, date, rows, cols
def getLineExposure(directory):
    try:
        for file in os.listdir(directory):
            if 'DataAcq.log' in file:
                with open(f"{directory}\\{file}", 'rt') as acqLog:
                    lines = acqLog.readlines()
                    for line in lines:
                        if 'Line Exposure' in line:
                            return line
    except Exception as e:
        return ''
    return ''
#Message Boxes-----------------------------------------------------------------------------------------------------------------------------
def errorMessage(title = '', text = ''):
    message = QMessageBox()
    message.setIcon(QMessageBox.Critical)
    message.setWindowTitle(title)
    message.setText(text)
    message.exec_()
    message.setFocus()

def successMessage(title = '', text = ''):
    message = QMessageBox()
    message.setIcon(QMessageBox.Information)
    message.setWindowTitle(title)
    message.setText(text)
    message.exec_()
    message.setFocus()

#Day2 Calc---------------------------------------------------------------------------------------------------------------------------------
def day2(directory):
    total_sensors = 0
    required_rerun = 0
    too_few = 0
    solo_sensors = []
    sn_list = []
    dir_list = os.listdir(directory)
    #Get list of solo sensors
    for folder in dir_list:
        folderDir = f"{directory}\\{folder}"
        print(f"Checking: {folderDir}")
        if os.path.isdir(folderDir) and folder[0:2] == "SN":
            print('Valid folder...')
            if folder[0:8] in sn_list:
                continue
            if not os.path.isfile(f"{folderDir}\\AccDataAcq.log"):
                continue
            with open(f"{folderDir}\\AccDataAcq.log",'rt') as log:
                lines = log.readlines()
            solo = False
            print(lines[6])
            if "916-" in lines[6] or "917-" in lines[6]:
                solo = True
            if "916-01" in lines[6]:
                solo = False
            if solo == True:
                print('Solo sensor:')
                if folder[0:8] not in sn_list:
                    sn_list.append(folder[0:8])
                    solo_sensors.append([folder[0:8],0]) #list pair as [SNXXXXXX,# of occurrences]
                    print(f"{folder[0:8]} added.\n")
            else:
                print(f"{folder[0:8]} is not Solo.\n")
    print('Found solo sensors----------------------------------------------------------------------')
    print(solo_sensors)
    print('----------------------------------------------------------------------------------------')
    #Check how many folders each has, update sensor[1]
    total_sensors = len(solo_sensors)
    for sensor in solo_sensors:
        for folder in dir_list:
            folderDir = f"{directory}\\{folder}"
            if sensor[0] in folder and os.path.isfile(f"{folderDir}\\AccDataAcq.log"):
                sensor[1] += 1
        if sensor[1] > 3:
            required_rerun += 1
        elif sensor[1] < 3:
            too_few += 1
    print(solo_sensors)
    print(f"Solo sensors: {total_sensors}")
    print(f"# requiring extra thermal: {required_rerun}")
    print(f"Sensors with < 3 folders: {too_few}")

   #Utility functions----------------------------------------------------------------------------------------------------------------------
def getFileFolder(sn = '', tcompDir = ''):
    selectedFile = ''
    selectedFolder = ''
    try:
        if sn == '':
            raise Exception("No serial number was entered")
        elif 'SN' not in sn:
            raise Exception("Serial number must be entered with SNXXXXXX format.")
        #Find the tcomp file for the specified SN
        snPrefix = sn[0:5]
        for folder in os.listdir(tcompDir):
            if snPrefix in folder:
                selectedFolder = tcompDir + '\\' + folder
                break
        for file in os.listdir(selectedFolder):
            if sn in file:
                selectedFile = selectedFolder + '\\' + file
                break
    except:
        errorMessage("Error","Unable to find the folder or file for the specified SN.")
    return selectedFile, selectedFolder