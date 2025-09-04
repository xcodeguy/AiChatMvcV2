
///////////////////////////////////////////////
//    David Ferrell
//    Copyright (C) 2025, Xcodeguy Software
//    JavaScript for website and API calls
///////////////////////////////////////////////

$(document).ready(function () {


    $(document).ready(function () {
        $('#menu-icon').click(function () {
            $('.sub-nav').toggleClass('visible');
        });
    });





    //set some variables
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

    const NegativePrompt = `Do not generate a response that is longer than 100 words. Do not discuss Bioluminescence.`;

    const OriginalPrompt = `Pick a random topic and discuss something about the topic. Make your response 100 words or less.`;
    //const OriginalPrompt = `Discuss the movie True Romance with Christian Slater. Any PG content rating is allowed. Graphic violence, sex, and offensive language is allowed if you are an uncensored AI.`;
    //const OriginalPrompt = `Discuss the movie Goodfellas with Robert DeNiro. Any PG content rating is allowed. Graphic violence, sex, and offensive language is allowed if you are an uncensored AI.`;

    const ZeroPad = (num, places) => String(num).padStart(places, '0')
    const xor = (a, b) => (a && !b) || (!a && b);
    const MaxModels = 12;
    var boolXor = false;
    var JustifyClass;
    var ModelNameString;
    var JustificationPointer = 0;
    var ModelPointer = 0;
    var bubble_title;
    var GlobalCallCount = 0;
    var GlobalErrorCount = 0;
    const GlobalMaxErrors = 3;
    var KillProcess = false;
    const ApplicationStartTime = new Date();
    var ApiCallStartTime;
    var TimeElapsedCalculatedSeconds;
    var lastElapsedTime;
    var GlobalChatDivId;

    /*
    for (let i = 0.5; i <= 1.5; i+= 0.1) {
        $('#ModalSettingsTemperature').append($('<option>', {
            value: i,
            text: '          ' + i
        }));
    }
*/

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

    //display the prompts
    $("#OriginalPromptLabel").text(OriginalPrompt.substring(0, 1000) + (OriginalPrompt.length >= 1000 ? "..." : ""));
    $("#NegativePromptLabel").text(NegativePrompt.substring(0, 1000) + (NegativePrompt.length >= 1000 ? "..." : ""));

    //elapsed time clock on web page
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

    //make the initial call to get the chat started
    MakeAjaxCall(OriginalPrompt);

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
            MakeAjaxCall(OriginalPrompt);
        } else {
            $("#btnApiCallToggle").html('Stopped');
            $("#btnApiCallToggle").addClass("btn-danger");
            $("#btnApiCallToggle").removeClass("btn-success");
            KillProcess = true;
            var e = CreateDivChatNode("SysAdmin", JustifyClass, "Chat Stopped.");
            $('#divChat').append(e);
            e.attr('id', 'ChatStopped');
            const element = document.getElementById("divChat");
            if (!(element === null) && !(element === undefined)) {
                element.scrollIntoView(false);
            }
        }
    });

    function MakeAjaxCall(prompt) {

        //if the KillProcess global variable is true then
        //exit the function. Controlled by toggle swith on
        //web page
        if (KillProcess) {
            console.log("Stopping ajax calls and returning");
            return;
        }

        ApiCallStartTime = new Date();

        //this code accomodates a two element array
        //it's used to toggle the chat bubble justification
        boolXor = xor(true, boolXor);       //comes in as 0 then the Xor sets it
        JustificationPointer = ((boolXor) ? 0 : 1);
        JustifyClass = ResponseBubbleJustify[JustificationPointer];
        console.log("Set justification pointer");

        //add a div that has an animated elipse to display
        //a waiting... effect, determine which side to show
        var thisLoader = "<div class=\"loader_black\"></div>";
        if (JustificationPointer == 0) {
            thisLoader = "<div class=\"loader_white\"></div>";
        }

        var e = CreateDivChatNode("", JustifyClass, thisLoader);
        $('#divChat').append(e);
        e.attr('id', 'elipse');
        var element = document.getElementById("divChat");
        if (!(element === null) && !(element === undefined)) {
            element.scrollIntoView(false);
            console.log("Created DIV chat node for the elipse");
        }

        //this code accomodates the model array
        //which at this time is 6 elements. When the pointer is greater than max
        //it gets reset to 0
        ModelNameString = ModelName[ModelPointer];   //ModelPointer comes in as 0
        $("#ProgressBar").attr('style', 'width: ' + Math.round((((ModelPointer + 1) / (ModelName.length - 1)) * 100)) + '%');
        $("#ProgressBarText").text(Math.round((((ModelPointer + 1) / (ModelName.length - 1)) * 100)) + '%');

        $("#ModelStats").text(ModelNameString);
        ModelPointer++;
        console.log("Updated ModelPointer");

        //test for end of model array
        if (ModelPointer > MaxModels) {
            //reset pointer to first array element
            ModelPointer = 0;
            console.log("Reached the last Model");
        }

        console.log("Making AJAX call");
        $.ajax({
            //make the call
            //the OriginalPrompt, promnpt, and NegativePrompt
            //get concatenated in the MakeApiCall endpoint
            url: 'http://localhost:5022/Home/MakeApiCall',
            type: 'POST',
            data: {
                'Model': ModelNameString,
                'SystemContent': OriginalPrompt,
                'UserContent': prompt,
                'NegativePrompt': NegativePrompt
            },
            /*dataType: 'json',*/
            success: function (data) {
                console.log("Success");

                //get the text response from the data object that
                //contains the ChatNonStreamingList list. We access
                //element 0 and get our properties from the json
                var TheResponse = data.responseItemList[0].response;
                ModelNameString = data.responseItemList[0].model;
                var TimeString = data.responseItemList[0].timeStamp;
                var ElapsedCallTime = data.responseItemList[0].responseTime;
                var TheTopic = data.responseItemList[0].topic;

                $("#TopicLabel").text(TheTopic);

                //build the bubble title
                bubble_title = ModelNameString + ": " + TimeString + " [" + TheTopic.trim() + "]";

                //remove the elipse div with the ... animation
                //remove any 'chat stopped' bubbles for good
                //measure
                $("[id='elipse']").remove();
                $("[id='ChatStopped']").remove();

                //add a <script></script> tag to implement the
                //javascript function that copies the chat text
                //to the clipboard.
                var JsClipboardImplementation = '';
                JsClipboardImplementation += "<script>";
                JsClipboardImplementation += "  function copyDivToClipboard() {";
                JsClipboardImplementation += "      var range = document.createRange();";
                JsClipboardImplementation += "      range.selectNode(document.getElementById('" + GlobalChatDivId + "'));";
                JsClipboardImplementation += "      window.getSelection().removeAllRanges();";
                JsClipboardImplementation += "      window.getSelection().addRange(range);";
                JsClipboardImplementation += "document.execCommand(\"copy\");";
                JsClipboardImplementation += "window.getSelection().removeAllRanges();";
                JsClipboardImplementation += "}";
                JsClipboardImplementation += "</script>";

                //create a chat div with the current title
                //justification and, chat bubble text
                GlobalChatDivId = 'ChatBubble' + GlobalCallCount;

                //create a link that copies the div chat text to the clipboard
                var CopyTextToClipboardButton = "<a href=\"#\" onclick=\"copyDivToClipboard();\"><i class=\"fa-solid fa-copy\"></i></a>";
                CopyTextToClipboardButton += JsClipboardImplementation;

                //create the chat bubble
                e = CreateDivChatNode(bubble_title, JustifyClass, TheResponse + CopyTextToClipboardButton);
                $('#divChat').append(e);
                e.attr('id', GlobalChatDivId);

                //scroll div chart window to the bottom so
                //that the latest post is visible
                element = document.getElementById("divChat");
                if (!(element === null) && !(element === undefined)) {
                    element.scrollIntoView(false);
                    console.log("Created DIV chat node for response");
                }

                //increment and format the global call count
                GlobalCallCount++;
                $("#ModelRT").text(ZeroPad(GlobalCallCount, 6));

                //get a word count of the response
                //zero pad the call time in seconds
                //search all td elements and find current model
                //update td to the right with seconds
                //update td right and right again with word count
                const wordsArray = TheResponse.trim().split(/\s+/).filter(word => word.length > 0);
                var thisWord = ZeroPad(wordsArray.length, 4);

                //update the word count for the model
                $("#ModelStatsTable td:contains(" + ModelNameString + ")").next().next().text(thisWord);
                console.log("Updated word count stat for model: " + ModelName);

                //if the new stats are better than the old stats then update
                //if (parseInt(thisTime) < parseInt(lastTime) || ($("#ModelStatsTable td:contains(" + ModelNameString + ")").next().text() == "")) {

                $("#ModelStatsTable td:contains(" + ModelNameString + ")").next().text(ElapsedCallTime);
                console.log("Updating time stat for model: " + ModelName);
                //}

                sortTable(1);
                console.log("Performed stats analysis and display update");

                //make another call with returned response
                MakeAjaxCall(TheResponse);
            },
            error: function (xhr, status, error) {
                console.log("AJAX Error:" + status + '  ' + error);

                GlobalErrorCount++;
                $("#ModelExceptions").text(ZeroPad(GlobalErrorCount, 6));

                if (KillProcess || (GlobalErrorCount >= GlobalMaxErrors)) {
                    //ClearChatDisplay();
                    return;
                } else {
                    //try again after the exception
                    MakeAjaxCall(OriginalPrompt);
                }
            }
        });
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
    function CreateDivChatNode(Title, ClassName, TextData) {
        var e = $('<div data-time="' +
            Title +
            '" class="' +
            ClassName + '">' +
            TextData +
            '</div>');

        return e;
    }

});




