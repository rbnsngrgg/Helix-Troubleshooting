import math
from PIL import Image,ImageChops,ImageDraw
from statistics import mean

class HelixImage(object):
    """Base class for TIF Images"""
    def __init__(self):
        self.rows = 0
        self.columns = 0
        self.name = ""
        self.location = ""

class LaserImage(HelixImage):
    #Class for grayscale laser line images
    def __init__(self):
        super().__init__()
        self.zValue = 0
        self.lineAngle = None

        self.CGs = []
        self.cgAvgLeft = 0
        self.cgAvgRight = 0
        self.cgAvg = 0

        self.widths = []
        self.widthAvgLeft = 0
        self.widthAvgRight = 0
        self.widthAvg = 0

        self.scores = []
        self.overallFocus = None
        self.hasLine = False

        self.image = None
        self.pixel = None
        self.logLines = []
        self.thresholdPercent = 20

    def startAnalysis(self):
        self.image = Image.open(self.location)
        self.pixel = self.image.load()
        for x in range(0,int(self.columns)):
            startPix = None
            endPix = None
            pixels = []
            peak = 0
            peakRange = None 
            #Pixels in col
            for y in range(0,int(self.rows)):
                #Get peak in col to get threshold value
                if self.pixel[x,y] > peak:
                    peak = self.pixel[x,y]
                    if y < 19:
                        peakRange = (0,y+20)
                    elif y > 1179:
                        peakRange = (y-20,1199)
                    else:
                        peakRange = (y-20,y+20)
            threshold = (self.thresholdPercent/100) * peak
            if peakRange == None:
                continue
            for y in range(0,int(self.rows)):
                if peakRange[0] <= y <= peakRange[1]:
                    #[(y-coord,value)]
                    value = self.pixel[x,y]
                    #Gather relevant pixels in col
                    if value > threshold and startPix == None:
                        startPix = y
                        pixels.append((y,value))
                    elif value < threshold and startPix != None and endPix == None:
                        endPix = y-1
                    elif value > threshold and endPix == None:
                        pixels.append((y,value))
                    elif value > threshold and endPix != None:
                        endPix = None
                        pixels.append((y,value))
                    if endPix == None and y == int(self.rows)-1:
                        continue
                if y == int(self.rows)-1:
                    #Generate CG and line width, add to list
                    if peak > ((self.thresholdPercent/100) * 255):
                        cg = self.getCG(pixels,startPix)
                        self.CGs.append(cg)
                        focusScore = self.getFocus(pixels)
                        self.scores.append(focusScore)
                        self.widths.append(len(pixels))
                        self.logLines.append(f'{cg}\t{x}\t{len(pixels)}\t{peak}\t{focusScore}\t{pixels}\n')
        self.lineData()

    def getFocus(self,pixels):
        peak = 0
        run1 = 1
        run2 = 1
        for pixel in pixels:
            if pixel[1] > peak:
                peak = pixel[1]
        rise = peak
        #First side
        for index,pixel in enumerate(pixels):
            if pixel[1] == peak and index !=0:
                run1 += pixel[0]-pixels[0][0]
                break
        score1 = rise/run1
        pixels.reverse()

        #Second Side
        for index,pixel in enumerate(pixels):
            if pixel[1] == peak and index != 0:
                run2 += pixels[0][0]-pixel[0]
                break
        score2 = rise/run2

        score = round(score1 + score2,2)
        return score

    def getCG(self,pixels,startPix):
        cgTotal = 0
        cgMoment = 0
        for p in pixels:
            cgTotal += p[1]
        for i in range(0,len(pixels)):
            momentV1 = pixels[i][1]
            momentV2 = pixels[i][0]-startPix
            cgMoment += momentV1 * momentV2
        cg = round(startPix + (cgMoment/cgTotal),1)
        return cg

    def lineData(self):
        #Check that the line is valid
        if len(self.CGs) < (int(self.columns)*0.25):
            self.logLines = self.logLines[0:1]
            self.logLines.append(f"\nNo laser line found.\nThreshold = {self.thresholdPercent}%")
            return
        else:
            #Get CG averages
            #print(f"CGs: {len(cgs)}")
            left = []
            right = []
            for index,cg in enumerate(self.CGs):
                middle = (len(self.CGs)/2) - 1
                if index < middle:
                    left.append(cg)
                else:
                    right.append(cg)
            self.cgAvgLeft = round(sum(left) / len(left),2)
            self.cgAvgRight = round(sum(right) / len(right),2)
            self.cgAvg = round(sum(self.CGs) / len(self.CGs),2)

            #Get width averages
            left = []
            right = []
            for index,width in enumerate(self.widths):
                middle = (len(self.widths)/2) - 1
                if index < middle:
                    left.append(width)
                else:
                    right.append(width)

            self.widthAvgLeft = round(sum(left) / len(left),2)
            self.widthAvgRight = round(sum(right) / len(right),2)
            self.widthAvg = round(sum(self.widths) / len(self.widths),2)

            #Angle and focus
            self.lineAngle = round(math.degrees(math.atan((self.CGs[-1] - self.CGs[0])/self.columns)),2)
            self.overallFocus = round(mean(self.scores),2)
            #Log entry
            self.logLines.append(f"\nCG Count:\t{len(self.CGs)}\n")
            self.logLines.append(f"\nCG Avg:\t{self.cgAvg}\nCG Avg Left:\t{self.cgAvgLeft}\nCG Avg Right:\t{self.cgAvgRight}\n")
            self.logLines.append(f"\nWidth Avg:\t{self.widthAvg}\nWidth Avg Left:\t{self.widthAvgLeft}\nWidth Avg Right:\t{self.widthAvgRight}\n")
            self.logLines.append(f"\nLine angle degrees:\t{self.lineAngle}\n")
            self.logLines.append(f"\nLine focus score:\t{self.overallFocus}\n")
