DROP PROCEDURE sp_create_table_response;
CREATE PROCEDURE sp_create_table_response (a  VARCHAR(10), b  VARCHAR(10))
    BEGIN
        DECLARE BoolTableExists INT;
        SET BoolTableExists =  EXISTS(SELECT * 
                                FROM information_schema.tables
                                WHERE table_schema = 'WakeNbake' 
                                AND table_name = 'Response');

        IF BoolTableExists = 1 THEN
            DROP TABLE Response;
        END IF;
        
        CREATE TABLE Response (
            timestamp DATETIME NOT NULL,
            response LONGTEXT NOT NULL,
            model VARCHAR(100),
            topic VARCHAR(100),
            prompt TEXT,
            negative_prompt TEXT,
            active BOOLEAN,
            last_updated DATETIME NOT NULL,
            response_time DATETIME NOT NULL,
            word_count INT UNSIGNED NOT NULL
        );
    END