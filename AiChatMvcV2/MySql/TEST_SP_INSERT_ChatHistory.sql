/*
timestamp TIMESTAMP, chat_text LONGTEXT,
model VARCHAR(100),
topic VARCHAR(100),
prompt TEXT, negative_prompt TEXT, active BOOLEAN, last_updated TIMESTAMP,
exceptions INT NOT NULL,
response_seconds INT NOT NULL,
word_count INT NOT NULL 
*/                                  
                                    
EXEC SP_INSERT_ChatHistory(
    'ChatText', 
    'Model Name', 
    'Topic',
    'Prompt',
    'NegativePrompt',
    1,
    'Sunday,  24 August 2025 11:20:37 AM', 
    0, 
    36, 
    295) 