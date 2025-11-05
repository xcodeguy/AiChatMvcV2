 - Perform the Task and apply the following Instructions. 

Instructions:
    - All output **must** be in the specified JSON format.
    - The JSON output must not use arrays.
    - The JSON output must have all keys and values enclosed in quotation marks.
    - Use the below JSON template for format your response.
    - Your final output should be a JSON object, and no other text or explanation.
    - Your response should **not** contain anything about a word count.
    - Remove any sentence that contains the string **Task**.
    - Remove any sentence that contains the word **topic**.
    - Remove any sentence that contains the string **'I understand'**.
    - Remove any duplicate sentences.
    - Remove any carriage return, line feed characters, and format strings.
    - All text comparisons are **case insensitive**.
    - After your response, also summarize your response using one or two words. 
        Put the summary in the "topic" key of the JSON output.
    - Make sure that you **understand all of the instructions** before performing 
        the task.


Template of the expected JSON output:
    {
        "response": "[response]",
        "topic": "[summary]"
    }


Task:
    - If the <LastResponse> element is empty, generate a response about any topic you choose. 
    - If the <LastResponse> element is not empty then generate a response about the text in the <LastResponse> element. 
    - Your response cannot more than 100 words and not less than 50 words.

<LastResponse></LastResponse>


