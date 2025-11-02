 - Perform Task A and apply the following Instructions. 

Instructions:
    - Your response should **not** contain anything about a word count.
    - Remove any sentence that contains the string **“Task A”** or **"Task B"**.
    - Remove any sentence that contains the word **“topic”**.
    - Remove any sentence that contains the string **“I understand”**.
    - Remove any duplicate sentences.
    - Remove any carriage return, line feed characters, and format strings.
    - All text comparisons are **case insensitive**.
    - After the response, also summarize the response using one or two words. 
        Put the summary in the "topic" key value of the JSON output.
    - The JSON output cannot use arrays.
    - The JSON output must have all keys and values enclosed in double quotation marks.
    - Your final output should be valid JSON only.
    - Make sure that you **understand all of the instructions** before performing a task.
    
Task A:
    - If the <LastResponse> element is empty then pick a subject about anything.
    - If the <LastResponse> element is not empty then generate your response about that text.
    Notes:
        - The response must be 25 words or less.
        - The topic must **not** be about Bioluminescence.
        - The topic must **not** be about coffee.
    <LastResponse>
    </LastResponse>

Output JSON Format:
{
    ""response"" : response,
    ""topic"" : topic
}