@file:Suppress("unused")

package model

import com.jetbrains.rd.generator.nova.*

const val folder = "demo"

object DemoModel : Root() {

    val a = openclass("a") {
        property("prop1", PredefinedType.string)
    }

    init {
        property("a", a)
    }
}