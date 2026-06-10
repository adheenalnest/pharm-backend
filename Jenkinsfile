pipeline {
    agent any

    environment {
        CONTAINER_NAME  = 'pharmeasy-backend'
        IMAGE_NAME      = 'pharmeasy-backend'
        NETWORK_NAME    = 'pharmeasy-network'
        DB_CONTAINER    = 'pharmeasy-mysql'
        PORT_MAPPING    = '8081:8080'
        DOCKER_BUILDKIT = '0'

        // ── Secrets ────────────────────────────────────────────────────────
        // Add these in Jenkins → Manage Jenkins → Credentials → Global:
        //   pharmeasy-db-connstring       (Secret text)
        //   pharmeasy-jwt-secret          (Secret text)
        //   pharmeasy-smtp-password       (Secret text)
        //   pharmeasy-razorpay-keysecret  (Secret text)
    }

    stages {

        stage('Checkout') {
            steps {
                checkout scm
            }
        }

        stage('Ensure MySQL Running') {
            steps {
                script {
                    // Create network if it doesn't exist
                    bat "docker network create ${NETWORK_NAME} 2>nul || ver >nul"

                    def mysqlRunning = bat(
                        script: "docker inspect -f {{.State.Running}} ${DB_CONTAINER}",
                        returnStdout: true
                    ).trim()

                    if (!mysqlRunning.contains('true')) {
                        echo "MySQL container not running — starting it now."
                        bat "docker rm ${DB_CONTAINER} 2>nul || ver >nul"
                        bat """
                            docker run -d ^
                                --name ${DB_CONTAINER} ^
                                --network ${NETWORK_NAME} ^
                                -p 3307:3306 ^
                                --restart unless-stopped ^
                                -e MYSQL_ROOT_PASSWORD=root ^
                                -e MYSQL_DATABASE=pharmeasy ^
                                -v pharmeasy-mysql-data:/var/lib/mysql ^
                                mysql:8.0
                        """
                        echo "Waiting for MySQL to be ready..."
                        sleep(time: 20, unit: 'SECONDS')
                    } else {
                        echo "MySQL container is already running."
                    }
                }
            }
        }

        stage('Build Docker Image') {
            steps {
                bat "docker build --no-cache -t ${IMAGE_NAME}:latest -t ${IMAGE_NAME}:${BUILD_NUMBER} ."
            }
        }

        stage('Deploy Container') {
            steps {
                script {
                    // Stop and remove existing container if running
                    bat "docker stop ${CONTAINER_NAME} 2>nul || ver >nul"
                    bat "docker rm   ${CONTAINER_NAME} 2>nul || ver >nul"

                    withCredentials([
                        string(credentialsId: 'pharmeasy-db-connstring',       variable: 'DB_CONN'),
                        string(credentialsId: 'pharmeasy-jwt-secret',          variable: 'JWT_SECRET'),
                        string(credentialsId: 'pharmeasy-smtp-password',       variable: 'SMTP_PASS'),
                        string(credentialsId: 'pharmeasy-razorpay-keysecret',  variable: 'RAZORPAY_SECRET')
                    ]) {
                        bat """
                            docker run -d ^
                                --name ${CONTAINER_NAME} ^
                                --network ${NETWORK_NAME} ^
                                -p ${PORT_MAPPING} ^
                                --restart unless-stopped ^
                                -e ASPNETCORE_URLS=http://+:8080 ^
                                -e "ConnectionStrings__DefaultConnection=%DB_CONN%" ^
                                -e "Database__Provider=MySql" ^
                                -e "Database__ConnectionString=%DB_CONN%" ^
                                -e "Jwt__SecretKey=%JWT_SECRET%" ^
                                -e "Smtp__Host=smtp.gmail.com" ^
                                -e "Smtp__Port=587" ^
                                -e "Smtp__Username=adheenalnest@gmail.com" ^
                                -e "Smtp__Password=%SMTP_PASS%" ^
                                -e "Smtp__From=adheenalnest@gmail.com" ^
                                -e "Razorpay__KeyId=rzp_test_Syl5owGtFUehSW" ^
                                -e "Razorpay__KeySecret=%RAZORPAY_SECRET%" ^
                                -e "Razorpay__Currency=INR" ^
                                -e "Razorpay__CallbackUrl=http://localhost:4201/order-success" ^
                                -e "Razorpay__BookingCallbackUrl=http://localhost:4201/booking-success" ^
                                ${IMAGE_NAME}:latest
                        """
                    }
                }
            }
        }

        stage('Health Check') {
            steps {
                script {
                    sleep(time: 10, unit: 'SECONDS')
                    def status = bat(
                        script: "docker inspect -f {{.State.Running}} ${CONTAINER_NAME}",
                        returnStdout: true
                    ).trim()
                    if (!status.contains('true')) {
                        error "Container ${CONTAINER_NAME} failed to start. Check: docker logs ${CONTAINER_NAME}"
                    }
                    echo "Backend container is healthy and running on port 8081."
                }
            }
        }
    }

    post {
        success {
            echo "PharmEasy backend pipeline completed successfully! API is live at http://localhost:8081"
        }
        failure {
            echo "PharmEasy backend pipeline failed. Check the logs above for details."
            bat "docker logs ${CONTAINER_NAME} 2>nul || ver >nul"
        }
        always {
            bat "docker image prune -f 2>nul || ver >nul"
        }
    }
}
