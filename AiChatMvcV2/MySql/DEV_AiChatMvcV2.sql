
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
    78,
    450
);

SELECT COUNT(*) FROM Response;