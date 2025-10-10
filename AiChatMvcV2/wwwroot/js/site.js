
///////////////////////////////////////////////
//    David Ferrell
//    Copyright (C) 2025, Xcodeguy Software
//    JavaScript for website and API calls
///////////////////////////////////////////////

$(document).ready(async function () {

    //chat justification and model arrays
    const ResponseBubbleJustify = new Array("msg sent", "msg rcvd");
    const ModelName = new Array(
        "gemma3",
        "deepseek-r1",
        "mistral",
        "qwen3",
        "codellama",
        "llama3.1",
        "everythinglm",
        "llava",
        "codegemma",
        "phi3",
        "llama2-uncensored",
        "wizard-vicuna-uncensored",
        "dolphin-phi"
    );

    const ZeroPad = (num, places) => String(num).padStart(places, '0')
    const xor = (a, b) => (a && !b) || (!a && b);
    const MaxModels = 12;
    const GlobalMaxErrors = 3;
    const ApplicationStartTime = new Date();
    var KillProcess = false;
    var GlobalCallCount = 0;
    var GlobalErrorCount = 0;
    var TimeElapsedCalculatedSeconds = 0;
    var lastElapsedTime = 0;
    var GlobalChatDivId = "";
    var GlobalNegativePrompt = "";
    var GlobalOriginalPrompt = "";

    var boolXor = false;
    var JustifyClass = "";
    var ModelNameString = "";
    var JustificationPointer = 0;
    var ModelPointer = 0;
    var bubble_title = "";
    var thisLoader = "";
    var DivChatContentElement = "";
    var DivChatContainerElement = "";
    var TheResponse = "";
    var TimeString = "";
    var ElapsedCallTime = "";
    var TheTopic = "";
    var WavfileName = "";
    var TtsVoice = "";
    var ExceptionString = "";

    $("#ThemeDropdownId").change(function () {
        var selectedValue = $(this).val(); // Get the value of the selected option
        var selectedText = $(this).find('option:selected').text(); // Get the text of the selected option
        console.log("Selected Value: " + selectedValue);
        console.log("Selected Text: " + selectedText);
        $("#ThemeStyleSheetId").attr('href', selectedValue);

        switch (selectedText) {
            case "Light":
                $(".ThemeDropdownStyle").css("color", "#000000");
                break;
            case "Delphi":
                $(".ThemeDropdownStyle").css("color", "#ffffff");
                break;
            case "Solaris":
                $(".ThemeDropdownStyle").css("color", "#ffffff");
                break;
        }
    });

    $("#btnLlmSettings").click(function () {
    });

    $("#ThFasterColumn").click(function () {
        sortTable(1);
    });

    $("#ThWordColumn").click(function () {
        sortTable(2);
    });

    function sortTable(n) {
        var table, rows, switching, i, x, y, shouldSwitch;
        table = document.getElementById("ModelStatsTable");
        switching = true;
        /* Make a loop that will continue until
        no switching has been done: */
        while (switching) {
            // Start by saying: no switching is done:
            switching = false;
            rows = table.rows;
            /* Loop through all table rows (except the
            first, which contains table headers): */
            for (i = 1; i < (rows.length - 1); i++) {
                // Start by saying there should be no switching:
                shouldSwitch = false;
                /* Get the two elements you want to compare,
                one from current row and one from the next: */
                x = rows[i].getElementsByTagName("TD")[n];
                y = rows[i + 1].getElementsByTagName("TD")[n];
                // Check if the two rows should switch place:
                if (x.innerHTML.toLowerCase() > y.innerHTML.toLowerCase()) {
                    // If so, mark as a switch and break the loop:
                    shouldSwitch = true;
                    break;
                }
            }
            if (shouldSwitch) {
                /* If a switch has been marked, make the switch
                and mark that a switch has been done: */
                rows[i].parentNode.insertBefore(rows[i + 1], rows[i]);
                switching = true;
            }
        }
    }

    //elapsed time clock on web page
    /*
    setInterval(function () {
        if (KillProcess) {
            lastElapsedTime = new Date();
        }
        var str = GetElapsedTime(ApplicationStartTime);
        if (KillProcess) {
            return;
        }
        $('#RealTimeClock').text(str);
    }, 1000); // 1000 milliseconds = 1 second 
    */

    //calculates elapsed time
    function GetElapsedTime(StartTime) {
        var CurrentTime = new Date();
        if (KillProcess) {
            CurrentTime = lastElapsedTime;
        }
        var ElapsedTimeMilliseconds = (CurrentTime - StartTime);

        var TimeElapsedSeconds = ZeroPad(Math.trunc(ElapsedTimeMilliseconds / 1000), 2);
        var TimeElapsedMinutes = ZeroPad(Math.trunc(ElapsedTimeMilliseconds / (1000 * 60)), 2);
        var TimeElapsedHours = ZeroPad(Math.trunc(ElapsedTimeMilliseconds / (1000 * 60 * 60)), 2);

        if (TimeElapsedSeconds > 60) {
            TimeElapsedCalculatedSeconds = ZeroPad((TimeElapsedSeconds - (TimeElapsedMinutes * 60)), 2);
        } else {
            TimeElapsedCalculatedSeconds = TimeElapsedSeconds;
        }

        return TimeElapsedHours + ":" +
            TimeElapsedMinutes + ":" +
            TimeElapsedCalculatedSeconds;
    }

    function GetElapsedTimeSeconds(StartTime) {
        var CurrentTime = new Date();
        var ElapsedTimeMilliseconds = (CurrentTime - StartTime);
        var TimeElapsedSeconds = Math.trunc(ElapsedTimeMilliseconds / 1000);
        var TimeElapsedMinutes = Math.trunc(ElapsedTimeMilliseconds / (1000 * 60));

        if (TimeElapsedSeconds > 60) {
            TimeElapsedCalculatedSeconds = (TimeElapsedSeconds - (TimeElapsedMinutes * 60));
        } else {
            TimeElapsedCalculatedSeconds = TimeElapsedSeconds;
        }

        return TimeElapsedCalculatedSeconds;
    }

    BuildModelStatsTable();

    function BuildModelStatsTable() {
        $('#ModelStatsTable > tbody').html('');

        //build the model list on the dashboard
        for (var i = 0; i < ModelName.length; i++) {

            var tr = $("<tr class=\"ModelStatsTableRow\"></tr>");
            tr.attr('id', 'ModelStatsTableRow' + i);

            var ModelNameCell = $("<td style='text-align: left;' class=\"ModelStatsTableCell\">" + ModelName[i] + "</td>");
            ModelNameCell.attr('id', 'ModelStatsTablCell1_' + i);

            var ModelTimeCell = $("<td class=\"ModelStatsTableCell\"></td>");
            ModelTimeCell.attr('id', 'ModelStatsTablCell2_' + i);

            var ModelWordCountCell = $("<td class=\"ModelStatsTableCell\"></td>");
            ModelWordCountCell.attr('id', 'ModelStatsTablCell3_' + i);

            tr.append(ModelNameCell, ModelTimeCell, ModelWordCountCell);
            $('#ModelStatsTable tbody:last').append(tr);
        }
    }

    //returns X number of words from string
    function getSubstringByWordCount(str, numWords) {
        const wordsArray = str.trim().split(/\s+/).filter(word => word.length > 0);
        const selectedWords = wordsArray.slice(0, numWords);
        return selectedWords.join(' '); // Join the selected words back into a string
    }

    //clear the chat window, reset the stats
    //and start a new chat session with the 
    //seed prompt
    $("#btnClearChatWindow").click(function () {
        ClearChatDisplay();
        BuildModelStatsTable();
        ModelPointer = 0;
    });

    //when toggle button is clicked, turn the API calls on or off
    $("#btnApiCallToggle").click(function () {
        $("[id='elipse']").remove();
        $("[id='ChatStopped']").remove();

        if (KillProcess) {
            KillProcess = false;
            $("#btnApiCallToggle").html('Started');
            $("#btnApiCallToggle").addClass("btn-success");
            $("#btnApiCallToggle").removeClass("btn-danger");
            CallApiEndpoint(OriginalPrompt);
        } else {
            $("#btnApiCallToggle").html('Stopped');
            $("#btnApiCallToggle").addClass("btn-danger");
            $("#btnApiCallToggle").removeClass("btn-success");
            KillProcess = true;
            var e = CreateDivChatContent("Dave says...", JustifyClass, "Chat Stopped.");
            $('#divChat').append(e);
            e.attr('id', 'ChatStopped');
            const element = document.getElementById("divChat");
            if (!(element === null) && !(element === undefined)) {
                element.scrollIntoView(false);
            }
        }
    });

    //GetPrompts() is called to get the two prompts
    //from appsettings.json via the HomeController.cs
    //The prompts are stored in global variables
    //When the last prompt is fetched, the success:
    //handler calls CallApiEndpoint() to start the
    //process of calling the models in a round robin fashion
    var result = await GetPrompts();

    function CallApiEndpoint(prompt) {

        //if the KillProcess global variable is true then
        //exit the function. Controlled by toggle switch on
        //web page
        if (KillProcess) {
            console.log("Stopping ajax calls because Running/Stopped button was clicked");
            return;
        }

        //this code accomodates a two element array
        //it's used to toggle the chat bubble justification
        //i.e. left or right side of the chat window
        //Xor the current value with true to toggle
        //the value each time this function is called   
        boolXor = xor(true, boolXor);       //comes in as 0 then the Xor sets it
        JustificationPointer = ((boolXor) ? 0 : 1);
        JustifyClass = ResponseBubbleJustify[JustificationPointer];

        //This code accomodates the model array
        //When the pointer is greater than max it gets reset to 0
        //so the models get called in a round robin fashion
        //Get the model name from the array
        ModelNameString = ModelName[ModelPointer];   //ModelPointer comes in as 0
        $("#ProgressBar").attr('style', 'width: ' + Math.round((((ModelPointer + 1) / (ModelName.length - 1)) * 100)) + '%');
        $("#ProgressBarText").text(Math.round((((ModelPointer + 1) / (ModelName.length - 1)) * 100)) + '%');
        $("#ModelStats").text(ModelNameString);
        ModelPointer++;

        //test for end of model array
        //If the ModelPointer is greater than the maximum number of models
        //then reset it to 0 so the next model called is the first
        //this creates a round robin effect
        if (ModelPointer > MaxModels) {
            //reset pointer to first array element
            ModelPointer = 0;
        }

        //add a div that has an animated elipse to display
        //a waiting... effect, determine which side to show
        //it on based on the JustificationPointer
        thisLoader = "<div class=\"loader_black\"></div>";
        if (JustificationPointer == 0) {
            thisLoader = "<div class=\"loader_white\"></div>";
        }

        //create the chat bubble with the elipse animation
        //and scroll the chat window to the bottom
        //so that the latest post is visible    
        DivChatContentElement = CreateDivChatContent(ModelNameString, JustifyClass, thisLoader);
        $('#divChat').append(DivChatContentElement);
        DivChatContentElement.attr('id', 'elipse');
        DivChatContainerElement = document.getElementById("divChat");
        if (!(DivChatContainerElement === null) && !(DivChatContainerElement === undefined)) {
            DivChatContainerElement.scrollIntoView(false);
        }

        console.log("Making api call");
        $.ajax({
            //make the call
            //the GlobalOriginalPrompt, promnpt, and NegativePrompt
            //get concatenated in the QueryModelForResponse endpoint
            url: 'http://localhost:5022/Home/QueryModelForResponse',
            type: 'POST',
            data: {
                'Model': ModelNameString,
                'SystemContent': GlobalOriginalPrompt,
                'UserContent': prompt,
                'NegativePrompt': GlobalNegativePrompt
            },
            /*dataType: 'json',*/
            success: function (data) {
                console.log("Success");

                //get the response items from the data object that
                //contains  the responseItemList list. We access
                //element 0 and get our properties from the json
                TheResponse = data.responseItemList[0].response;
                ModelNameString = data.responseItemList[0].model;
                TimeString = data.responseItemList[0].timeStamp;
                ElapsedCallTime = data.responseItemList[0].responseTime;
                TheTopic = data.responseItemList[0].topic;
                WavfileName = data.responseItemList[0].audioFilename;
                TtsVoice = data.responseItemList[0].ttsVoice;
                ExceptionString = data.responseItemList[0].exceptions;

                //method that updates the web UI if call is successful
                //or not. It is also used in the error: handler
                //to update the web page with the error message
                //The backend services throw exceptions into
                //the Exceptions property of the data.responseItemList[0]
                //The data.responseItemList[0] is handled and returned by 
                //the HomeController.cs
                if (ExceptionString.trim() != "") {
                    console.log(ExceptionString);
                    TheTopic = "Service Exception!";
                    UpdateWebUiElements(ExceptionString, false);
                    DivChatElementForException = document.getElementById(GlobalChatDivId);
                    DivChatElementForException.style.backgroundColor = "#ff0000";
                    DivChatElementForException.style.color = "#ffffff";

                    DisplayExceptionCountStatistic();
                }
                else {
                    UpdateWebUiElements(TheResponse, true);
                }

                //make another call with returned response
                console.log("Making recursive api call");
                CallApiEndpoint(TheResponse);
            },
            error: function (xhr, status, error) {
                console.log("Error:" + xhr.statusText);

                //method that updates the web UI if call is successful
                //or not. It is also used in the success: handler
                //to update the web page with the model meta-data
                TheTopic = "Http Exception!";
                UpdateWebUiElements(xhr.statusText, false);
                DivChatElementForException = document.getElementById(GlobalChatDivId);
                DivChatElementForException.style.backgroundColor = "#ff0000";
                DivChatElementForException.style.color = "#ffffff";

                DisplayExceptionCountStatistic();

                //-1 overrides the max exception logic
                if (GlobalMaxErrors == -1) {
                    console.log("GlobalMaxErros=-1: Ignoring retry logic and calling CallApiEndpoint().");
                    CallApiEndpoint(GlobalOriginalPrompt);
                } else if (KillProcess || (GlobalErrorCount >= GlobalMaxErrors)) {
                    return;
                } else {
                    //try again after the exception
                    CallApiEndpoint(GlobalOriginalPrompt);
                }
            },
            complete: function () {
                console.log("Done\n\n");
            }
        });
    }

    //updates the web UI elements
    //called from both the success: and error: handlers
    //of the ajax call
    function UpdateWebUiElements(DivText, PlaySpeechFile = true) {

        //check the text for null or empty, set flag if true
        //which will be used in the below code for making div
        //element red with white text
        var EmptyResponseOrException = false;
        if (DivText == null || DivText == undefined || DivText.trim() == "") {
            DivText = "Empty Response or Exception!";
            EmptyResponseOrException = true;
        }

        //update the prompt table topic cell on the web page
        $('#TopicLabel').fadeOut('slow', function () {
            $('#TopicLabel').fadeIn('slow', function () {

            });
        })
        $("#TopicLabel").text(TheTopic);

        //build the bubble title
        bubble_title = ModelNameString + ": " + TimeString + " [" + TheTopic.trim() + "] [" + TtsVoice + "]";

        //remove the elipse div with the ... animation
        //remove any 'chat stopped' bubbles for good
        //measure
        $("[id='elipse']").remove();
        $("[id='ChatStopped']").remove();

        //create a chat div with the current title
        //justification and, chat bubble text
        GlobalChatDivId = 'ChatBubble' + GlobalCallCount;

        //create the chat bubble
        DivChatContentElement = CreateDivChatContent(bubble_title, JustifyClass, DivText);
        $('#divChat').append(DivChatContentElement);
        DivChatContentElement.attr('id', GlobalChatDivId);

        //if flag is set make the background red and the text white
        //to indicate an error
        if (EmptyResponseOrException) {
            DivChatElementForException = document.getElementById(GlobalChatDivId);
            DivChatElementForException.style.backgroundColor = "#ff0000";
            DivChatElementForException.style.color = "#ffffff";
        }

        //scroll div chart window to the bottom so
        //that the latest post is visible
        DivChatContainerElement = document.getElementById("divChat");
        if (!(DivChatContainerElement === null) && !(DivChatContainerElement === undefined)) {
            DivChatContainerElement.scrollIntoView(false);
        }

        //increment and format the global call count
        GlobalCallCount++;
        $("#ModelRT").text(ZeroPad(GlobalCallCount, 6));

        //UPDATE THE STATS TABLE
        //get a word count of the response
        //zero pad the call time in seconds
        //search all td elements and find current model
        //update td to the right with seconds
        //update td right and right again with word count
        const wordsArray = DivText.trim().split(/\s+/).filter(word => word.length > 0);
        var thisWord = ZeroPad(wordsArray.length, 4);
        $("#ModelStatsTable td:contains(" + ModelNameString + ")").next().text(ElapsedCallTime);
        $("#ModelStatsTable td:contains(" + ModelNameString + ")").next().next().text(thisWord);

        //sort the table by call time
        sortTable(1);

        if (PlaySpeechFile) {
            $.ajax({
                //make the call to play the audio file
                url: 'http://localhost:5022/Home/PlaySpeechFile',
                type: 'POST',
                success: function (response) {

                },
                error: function (xhr, status, error) {
                    TheTopic = "Audio Exception!";
                    UpdateWebUiElements(xhr.statusText, false);
                    DivChatElementForException = document.getElementById(GlobalChatDivId);
                    DivChatElementForException.style.backgroundColor = "#ff0000";
                    DivChatElementForException.style.color = "#ffffff";

                    DisplayExceptionCountStatistic();
                }
            });
        }
    }

    //cleans up chat display
    function ClearChatDisplay() {
        $("[id='elipse']").remove();
        $("[id='ChatStopped']").remove();
        $("[id='ChatBubble" + GlobalCallCount + "']").remove();
        $("#divChat").html("");
        console.log("ClearChatDisplay()->GlobalErrorCount: " + GlobalErrorCount + ", KillProcess: " + KillProcess);
    }

    //creates a div node with class chat
    function CreateDivChatContent(Title, ClassName, TextData) {
        var e = $('<div data-time="' +
            Title +
            '" class="' +
            ClassName + '">' +
            TextData +
            '</div>');

        return e;
    }

    async function GetPrompts() {
        //get the startup prompt from the server
        $.ajax({
            url: 'http://localhost:5022/Home/GetStartupPrompt',
            type: 'POST',
            success: function (response1) {
                GlobalOriginalPrompt = response1;
                $("#OriginalPromptLabel").text(GlobalOriginalPrompt.substring(0, 1000) + (GlobalOriginalPrompt.length >= 1000 ? "..." : ""));

                //get the negative prompt from the server
                $.ajax({
                    url: 'http://localhost:5022/Home/GetNegativePrompt',
                    type: 'POST',
                    success: function (response2) {
                        GlobalNegativePrompt = response2;
                        $("#NegativePromptLabel").text(GlobalNegativePrompt.substring(0, 1000) + (GlobalNegativePrompt.length >= 1000 ? "..." : ""));

                        //call api with no prompt which will let the backend
                        //assemble just the Prompt ans Negative Prompt
                        CallApiEndpoint("");
                    },
                    error: function (xhr2, status, error) {
                        console.log("Error setting negative prompt: " + xhr2.responseText);
                    }
                });
            },
            error: function (xhr1, status, error) {
                console.log("Error setting startup prompt: " + xhr1.responseText);
            }
        });


        return true;
    }

    async function DisplayExceptionCountStatistic() {
        GlobalErrorCount++;
        $("#ModelExceptions").text(ZeroPad(GlobalErrorCount, 6));
    }
});




