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

Task 1:
    - If the <LastResponse> element is empty, generate a response about any topic you choose. 
    - If the <LastResponse> element is not empty then generate a response about the text in the <LastResponse> element. 
    - Your response cannot more than 100 words and not less than 50 words.
    - If the <LastResponse> element is not empty then compare

Task 2:
    - If the <LastResponse> element is not empty then compare the text 
    in the <LastResponse> element with your response and determine
    how relevant your response is to the <LastResponse> element text.
    - Your comparison should result in a weighted number between 1 and 5 and this number will be called "grade". 
    The more relevant the <LastResponse> is to your response should produce a
    higher grade.
    - Your grade can only be a number between 1 and 5.
    - Your grade should be added to the expected JSON output.


Template of the expected JSON output:
    {
        "response": "[response]",
        "topic": "[summary]",
        "grade": "[grade]"
    }




<LastResponse></LastResponse>


