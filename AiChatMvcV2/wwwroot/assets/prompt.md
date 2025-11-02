 - Perform the Task and apply the following Instructions. 

Instructions:
    - All output **must** be in the specified JSON format.
    - The JSON output must not use arrays.
    - The JSON output must have all keys and values enclosed in quotation marks.
    - The JSON example is an example only and should not be used as literal output.
    - Your final output should be a raw JSON string, and no other text or explanation.
    - Your response to the task should **not** contain anything about a word count.
    - Remove any sentence that contains the string **Task**.
    - Remove any sentence that contains the word **topic**.
    - Remove any sentence that contains the string **'I understand'**.
    - Remove any duplicate sentences.
    - Remove any carriage return, line feed characters, and format strings.
    - All text comparisons are **case insensitive**.
    - After the response, also summarize the response using one or two words. 
        Put the summary in the "topic" key value of the JSON output.
    - Make sure that you **understand all of the instructions** before performing 
        the task.
    
Example of the JSON output:
    {
        "response": "This is an example response.",
        "topic": "Some Topic"
    }

Task:
    - If the <LastResponse> element is empty then pick a subject about anything and 
        generate a response, and apply the Instructions to your response.
    - If the <LastResponse> element is not empty then generate your response about the text in 
        the <LastResponse> element, and apply the Instructions to your response.
    Notes:
        - The response must be 25 words or less.
        - The subject cannot be about Bioluminescence.
        - The subject cannot be about coffee.
    <LastResponse>
    </LastResponse>

    Output JSON Format:
    {
        "response" : [response],
        "topic" : [topic]
    }