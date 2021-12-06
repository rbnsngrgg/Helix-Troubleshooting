Helix Troubleshooting is a desktop application created to automate some of the procedures related to gathering data for, and troubleshooting, Helix sensors.

Main features:
-Removing duplicate ALS points (find the file in the rectification data, find duplicate entries in the file, delete the duplicates).

-Repairing algorithm errors in temperature compensation (t-comp) data. Search the text file for error codes, and use averages of the chronological data to fill measurement data gaps.

-Generate images that summarize the illuminted sphere TIFF images of rectification. Images with the same z-coordinates are layered into composite images.

-Laser line analysis. CGs for laser line images, line width, and line angle are calculated. Based on the slope to the highest saturated points across the line, a composite score is generated that represents the quality of the image, representing the combination of camera and laser focus.

-Staring dot removal for rectification images. The extraneous laser dot is located and removed from the images gathered during rectification. Rectification can resumed with the edited images.

-Temperature adjustment of t-comp data. In the event that the reference cycle temperature of a sensor's t-comp data does not match the normal operating temperature, it can be easily adjusted through this function, where the application will adjust each temperature entry in the file so that the average of the first 5 (reference cycle) matches the temperature specified.

-The application gathers data from each of the build fixtures and compiles them into a text file as tab-separated-values.
