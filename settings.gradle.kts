rootProject.name = "Advanced-Architecture"
include(":src:API-Gateway")
include(":src:edge-mqtt")
include(":src:Orchestrator")

// Map API-Gateway module to its nested 'src' directory where build.gradle.kts resides
project(":src:API-Gateway").projectDir = file("src/API-Gateway/src")
