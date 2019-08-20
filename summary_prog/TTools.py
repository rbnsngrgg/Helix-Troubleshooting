import os, shutil
from PySide2.QtWidgets import QMessageBox
from PIL import Image,ImageChops,ImageDraw


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
            #if "38500" in file:
                #print(toRemove)
            newText = []
            for line in data:
                keepLine = True
                for number in toRemove:
                    if str(number) in line.split()[5]:
                        keepLine = False
                if keepLine == True:
                    #if "38500" in file:
                        #print(line)
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
            #print(file1)
            #print(file2)
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
                    #print(file)
                    filename = file.split('\\')[-1]
                    image.close()
                    #os.unlink(file)
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
        if sn == '':
            raise Exception("No serial number was entered")
        elif 'SN' not in sn:
            raise Exception("Serial number must be entered with SNXXXXXX format.")
        #Find the tcomp file for the specified SN
        selectedFile = ''
        selectedFolder = ''
        snPrefix = sn[0:5]
        for folder in os.listdir(tcompDir):
            if snPrefix in folder:
                selectedFolder = tcompDir + '\\' + folder
                break
        for file in os.listdir(selectedFolder):
            if sn in file:
                selectedFile = selectedFolder + '\\' + file
                break

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


def errorMessage(title = '', text = ''):
    message = QMessageBox()
    message.setIcon(QMessageBox.Critical)
    message.setWindowTitle(title)
    message.setText(text)
    message.exec_()


def successMessage(title = '', text = ''):
    message = QMessageBox()
    message.setIcon(QMessageBox.Information)
    message.setWindowTitle(title)
    message.setText(text)
    message.exec_()