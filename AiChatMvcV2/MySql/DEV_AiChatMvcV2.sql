
 SELECT IF (EXISTS(SELECT * 
    FROM information_schema.tables
    WHERE table_schema = 'WakeNbake' 
        AND table_name = 'Response'),'Does exist','');

DROP TABLE Response;

DROP PROCEDURE citycount;

SELECT EXISTS(SELECT * 
    FROM information_schema.tables
    WHERE table_schema = 'WakeNbake' 
        AND table_name = 'Response')


SELECT IF (EXISTS(SELECT * 
    FROM information_schema.tables
    WHERE table_schema = 'WakeNbake' 
        AND table_name = 'Response'), 
        (
           1
        ), 0) 


CALL sp_create_table_response("A", "B");


CALL sp_insert_table_response(
    NOW(),
    'response',
    'model',
    'topic',
    'prompt',
    'negative_prompt',
    1,
    NOW(),
    '2025-08-26 00:00:02',
    450
);

SELECT COUNT(*) FROM Response;

TRUNCATE TABLE Response;

SELECT model, topic FROM Response 
ORDER BY model ASC, topic ASC;

DELETE FROM Response WHERE topic = ''

SELECT COUNT(*)
FROM `Response`;

SELECT * FROM Response WHERE timestamp >= '2025-08-26 01:00:00'

SELECT COUNT(topic) AS 'Hits', topic, timestamp FROM Response GROUP BY topic, timestamp ORDER BY Hits DESC, timestamp DESC;

SELECT * FROM Response WHERE Response LIKE '%Biolum%';

SHOW COLUMNS FROM Response;

ALTER TABLE Response
MODIFY topic TEXT;