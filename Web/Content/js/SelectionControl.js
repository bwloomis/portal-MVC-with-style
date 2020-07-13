// Summary-This function will change the background image of a <td> called via onClick of link
// td - Passes the td ID of selection
// MinTD - Minimum td number for range of selection 
// MaxTD - Maximum td number for range of selection
// InputQuestion - Question text field
// ImgPath - Path of images
// Returns-nothing
// Remarks - If the answer range starts at something other than 0 fix j=0 and n=0
function SetCircleClick(td,MinTD,MaxTD,InputQuestion,ImgPath,masterPrefix) 
{      
   //Get Question Number 
   var QNum;
 
   //Get Question Suffix (last 3 chars of string)
   var suffix = InputQuestion.substring(InputQuestion.length-3,InputQuestion.length)
   
   // how long is the question, this is depeneded on which section it comes from
   if (InputQuestion.length == 8 || InputQuestion.length == 9)
   {
       QNum = (InputQuestion.charAt((InputQuestion.indexOf('_') + 1)) + InputQuestion.charAt((InputQuestion.indexOf('_') + 2)));        
       for(var i=MinTD;i<=MaxTD;i++) 
       {
            document.getElementById(masterPrefix + "td"  + i + "_" + QNum).style.backgroundImage = "";  
       }
   }         
   else
   {
       QNum = InputQuestion.charAt((InputQuestion.indexOf('_') + 1));
       for(var i=MinTD;i<=MaxTD;i++) {
            document.getElementById(masterPrefix + "td" + i + "_" + QNum + "_" + suffix).style.backgroundImage = "";                                                                                
       }
   }
   
   
    // Set selected TD background to selected image 
    newImage = "url(" + ImgPath + "selected.gif)";
    document.getElementById(masterPrefix + td).style.backgroundImage = newImage;
    document.getElementById(masterPrefix + InputQuestion).value = td.charAt(2);
}

// Summary - This function will change the background image of a <td> called via onChange of textbox 
// MinTD - Minimum td number for range of selection 
// MaxTD - Maximum td number for range of selection
// InputQuestion - Question text field
// ImgPath - Path of images
// Returns-nothing
// Remarks - If the answer range starts at something other than 0 fix j=0 and n=0
function SetCircleChange(MinTD,MaxTD,InputQuestion,ImgPath,masterPrefix)
{
    newImage = "url(" + ImgPath + "selected.gif)";

   //Get Question Number (find _ add 1, booya) - gotta be a better way
   var QNum;
   
   //Get Question Suffix (last 3 chars of string)
   var suffix = InputQuestion.substring(InputQuestion.length-3,InputQuestion.length)
 
 
   //TODO: check the lengths probly can do this like above
   switch (InputQuestion.length)
   {
    case 7:
       QNum = InputQuestion.charAt((InputQuestion.indexOf('_') + 1));             
       for(var i=MinTD;i<=MaxTD;i++) 
       {
            document.getElementById(masterPrefix + "td" + i + "_" + QNum).style.backgroundImage = "";
       }
       // Set selected TD background to image
       for (var c=MinTD;c<=MaxTD;c++)
       {
           var questVal = document.getElementById(masterPrefix + InputQuestion).value;
           if (questVal && questVal != "" && questVal == c)
            {
                document.getElementById(masterPrefix + "td" + c + "_" + QNum).style.backgroundImage = newImage;
            }
       }       
       break;  
    case 8:
       QNum = InputQuestion.charAt((InputQuestion.indexOf('_') + 1));         
       for(var i=MinTD;i<=MaxTD;i++) 
       {
            document.getElementById(masterPrefix + "td" + i + "_" + QNum).style.backgroundImage = "";
       }
       // Set selected TD background to image
       for (var c=MinTD;c<=MaxTD;c++)
       {
           var questVal = document.getElementById(masterPrefix + InputQuestion).value;
           if (questVal && questVal != "" && questVal == c)
            {
                document.getElementById(masterPrefix + "td" + c + "_" + QNum).style.backgroundImage = newImage;
            }
       }       
       break;
    case 9:
       QNum = InputQuestion.substring((InputQuestion.indexOf('_') + 1));             
       for(var i=MinTD;i<=MaxTD;i++) 
       {
            document.getElementById(masterPrefix + "td" + i + "_" + QNum).style.backgroundImage = "";
       }
       // Set selected TD background to image
       for (var c=MinTD;c<=MaxTD;c++)
       {
           var questVal = document.getElementById(masterPrefix + InputQuestion).value;
           if (questVal && questVal != "" && questVal == c)
            {
                document.getElementById(masterPrefix + "td" + c + "_" + QNum).style.backgroundImage = newImage;
            }
       }       
       break;
   default:
       QNum = InputQuestion.charAt((InputQuestion.indexOf('_') + 1));
       for (var i = MinTD; i <= MaxTD; i++) {
           document.getElementById(masterPrefix + "td" + i + "_" + QNum + "_" + suffix).style.backgroundImage = "";
       }
       // Set selected TD background to image
       for (var c = MinTD; c <= MaxTD; c++) {
           var questVal = document.getElementById(masterPrefix + InputQuestion).value;
           if (questVal && questVal != "" && questVal == c) {
               document.getElementById(masterPrefix + "td" + c + "_" + QNum + "_" + suffix).style.backgroundImage = newImage;
           }
       }
   
   }

}
