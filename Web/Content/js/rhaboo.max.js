﻿! function a(b, c, d) {
    function e(g, h) {
        if (!c[g]) {
            if (!b[g]) {
                var i = "function" == typeof require && require;
                if (!h && i) return i(g, !0);
                if (f) return f(g, !0);
                var j = new Error("Cannot find module '" + g + "'");
                throw j.code = "MODULE_NOT_FOUND", j
            }
            var k = c[g] = {
                exports: {}
            };
            b[g][0].call(k.exports, function (a) {
                var c = b[g][1][a];
                return e(c ? c : a)
            }, k, k.exports, a, b, c, d)
        }
        return c[g].exports
    }
    for (var f = "function" == typeof require && require, g = 0; g < d.length; g++) e(d[g]);
    return e
}({
    1: [
        function (a, b, c) {
            "use strict";
            var d = function (a) {
                return null === a ? "null" : typeof a
            }, e = function (a) {
                return a
            }, f = function (a) {
                return function (b) {
                    return a
                }
            }, g = function (a) {
                return function (b) {
                    return a === b
                }
            }, h = function (a) {
                return function (b) {
                    return b.map(a)
                }
            }, i = function (a) {
                return function (b) {
                    return void 0 !== a[1] ? [a[0], a[1](b)] : [a[0]]
                }
            }, j = function (a) {
                return function (b) {
                    return function (c) {
                        for (var d = [], e = 0; e < b.length && e < c.length; e++) d[e] = b[e](a)(c[e]);
                        return d
                    }
                }
            }, k = function (a) {
                return function (a) {
                    return a.toString()
                }
            }, l = function (a) {
                return a ? function (a) {
                    return a.toString()
                } : function (a) {
                    return Number(a)
                }
            }, m = function (a) {
                return a ? function (a) {
                    return a ? "t" : "f"
                } : function (a) {
                    return "t" === a
                }
            }, n = function (a) {
                return f(a ? "" : null)
            }, o = function (a) {
                return f(a ? "" : void 0)
            }, p = function (a) {
                return function (b) {
                    return function (c) {
                        return function (d) {
                            return c ? b(c)(a(c)(d)) : a(c)(b(c)(d))
                        }
                    }
                }
            }, q = function (a) {
                return function (b) {
                    for (var c = [], d = 0, d = 0; d < a.length && b.length; b = b.substring(a[d++])) c.push(b.substring(0, a[d]));
                    return c.push(b), c
                }
            }, r = function (a) {
                return function (b) {
                    return function (c) {
                        return c ? function (a) {
                            return j(!0)(b)(a).join("")
                        } : function (c) {
                            return j(!1)(b)(q(a)(c))
                        }
                    }
                }
            }, s = function (a) {
                return function (b) {
                    return function (c) {
                        return c ? function (c) {
                            return j(!0)(b)(c).join(a)
                        } : function (c) {
                            return j(!1)(b)(c.split(a))
                        }
                    }
                }
            }, t = function (a, b) {
                return function (c) {
                    return function (d) {
                        return d ? function (d) {
                            return c(!0)(d).replace(RegExp(a, "g"), a + b)
                        } : function (d) {
                            return c(!1)(d.replace(RegExp(a + b, "g"), a))
                        }
                    }
                }
            }, u = function (a, b) {
                return function (c) {
                    return function (d) {
                        return function (e) {
                            return s(d ? a : RegExp(a + "(?!" + b + ")", "g"))(h(t(a, b))(c))(d)(e)
                        }
                    }
                }
            }, v = u(";", ":");
            b.exports = {
                typeOf: d,
                id: e,
                konst: f,
                eq: g,
                map: h,
                runSnd: i,
                string_pp: k,
                number_pp: l,
                boolean_pp: m,
                null_pp: n,
                undefined_pp: o,
                thru: j,
                chop: q,
                fixedWidth: r,
                sepBy: s,
                escape: t,
                sepByEsc: u,
                tuple: v,
                pipe: p
            }
        }, {}
    ],
    2: [
        function (a, b, c) {
            var d = a("./core"),
                e = e || {
                    pop: Array.prototype.pop,
                    push: Array.prototype.push,
                    shift: Array.prototype.shift,
                    unshift: Array.prototype.unshift,
                    splice: Array.prototype.splice,
                    reverse: Array.prototype.reverse,
                    sort: Array.prototype.sort,
                    fill: Array.prototype.fill
                }, f = function (a) {
                    return function () {
                        var b, c, f = void 0;
                        this._rhaboo && (c = this._rhaboo.storage, f = this._rhaboo.slotnum, b = this._rhaboo.refs, d.release(this, !0));
                        var g = e[a].apply(this, arguments);
                        return void 0 !== f && d.addRef(this, c, f, b), g
                    }
                };
            Array.prototype.push = function () {
                var a = this.length,
                    b = e.push.apply(this, arguments),
                    c = this.length;
                if (void 0 !== this._rhaboo && c > a) {
                    for (var f = a; c > f; f++) d.storeProp(this, f);
                    d.updateSlot(this)
                }
                return b
            }, Array.prototype.pop = function () {
                var a = this.length;
                void 0 !== this._rhaboo && a > 0 && d.forgetProp(this, a - 1);
                var b = e.pop.apply(this, arguments);
                return void 0 !== this._rhaboo && a > 0 && d.updateSlot(this), b
            }, Object.defineProperty(Array.prototype, "write", {
                value: function (a, b) {
                    Object.prototype.write.call(this, a, b), d.updateSlot(this)
                }
            }), Array.prototype.shift = f("shift"), Array.prototype.unshift = f("unshift"), Array.prototype.splice = f("splice"), Array.prototype.reverse = f("reverse"), Array.prototype.sort = f("sort"), Array.prototype.fill = f("fill"), b.exports = {
                compatibilityMode: d.compatibilityMode,
                persistent: d.persistent,
                perishable: d.perishable,
                inMemory: d.inMemory,
                construct: d.construct,
                algorithm: "sand"
            }
        }, {
            "./core": 3
        }
    ],
    3: [
        function (a, b, c) {
            (function (c) {
                "use strict";

                function d(a) {
                    return void 0 !== a && (w = !!a), w
                }

                function e(a) {
                    try {
                        a = c[a];
                        var b = "_rhaboo_test_" + Date.now().toString();
                        a.setItem(b, b);
                        var d = a.getItem(b);
                        if (a.removeItem(b), b !== d) throw new Error;
                        return !0
                    } catch (e) {
                        return !1
                    }
                }

                function f(a, b, c) {
                    return !a && (void 0 !== c && !c || !w && !c)
                }

                function g(a, b) {
                    return j(f(x, a, b) ? new M(a) : localStorage, a)
                }

                function h(a, b) {
                    return j(f(y, a, b) ? new M(a) : sessionStorage, a)
                }

                function i(a) {
                    return j(new M(a), a)
                }

                function j(a, b) {
                    var c = a.getItem(z + b);
                    if (c) {
                        var d = H(a)(!1)(c);
                        return A = {}, d[0]
                    }
                    var e = {};
                    Object.defineProperty(e, "_rhaboo", {
                        value: {
                            storage: a
                        },
                        writable: !0,
                        configurable: !0,
                        enumerable: !1
                    });
                    var f = r(e);
                    return a.setItem(z + b, F(a)(!0)(f)), f
                }

                function k(a) {
                    return function (b) {
                        if (void 0 !== A[b]) return r(A[b]);
                        var c = a.getItem(z + b);
                        if (void 0 === c || null === c) return void 0;
                        var d = G(!1)(c);
                        return Object.defineProperty(d[0], "_rhaboo", {
                            value: {
                                storage: a,
                                slotnum: b,
                                refs: 1,
                                kids: {}
                            },
                            writable: !0,
                            configurable: !0,
                            enumerable: !1
                        }), A[b] = d[0], d.length > 1 ? l(d[0], d[1][0], d[1][1]) : d[0]
                    }
                }

                function l(a, b, c) {
                    var d = a._rhaboo.storage.getItem(z + c);
                    if (void 0 === d || null === d) return a;
                    var e = H(a._rhaboo.storage)(!1)(d);
                    return a[b] = e[0], m(a, b, c), e.length > 1 ? l(a, e[1][0], e[1][1]) : a
                }

                function m(a, b, c) {
                    var d = void 0 !== a._rhaboo.prev ? a._rhaboo.kids[a._rhaboo.prev] : a._rhaboo;
                    a._rhaboo.kids[b] = {
                        slotnum: c,
                        prev: a._rhaboo.prev
                    }, d.next = a._rhaboo.prev = b
                }

                function n(a, b) {
                    var c = a._rhaboo.kids[b];
                    (a._rhaboo.kids[c.prev] || a._rhaboo).next = c.next, (a._rhaboo.kids[c.next] || a._rhaboo).prev = c.prev, delete a._rhaboo.kids[b]
                }

                function o(a, b) {
                    var c = [];
                    c.push(void 0 !== b ? a[b] : a);
                    var d = void 0 !== b ? a._rhaboo.kids[b] : a._rhaboo;
                    void 0 !== d.next && c.push([d.next, a._rhaboo.kids[d.next].slotnum]);
                    var e = (void 0 !== b ? H(a._rhaboo.storage) : G)(!0)(c);
                    try {
                        a._rhaboo.storage.setItem(z + d.slotnum, e)
                    } catch (f) {
                        throw p(f) && a._rhaboo.storage.removeItem(z + d.slotnum, e), console.log("Local storage quota exceeded by rhaboo"), f
                    }
                }

                function p(a) {
                    var b = !1;
                    if (a)
                        if (a.code) switch (a.code) {
                            case 22:
                                b = !0;
                                break;
                            case 1014:
                                "NS_ERROR_DOM_QUOTA_REACHED" === a.name && (b = !0)
                        } else -2147024882 === a.number && (b = !0);
                    return b
                }

                function q(a, b) {
                    if (void 0 === a._rhaboo.kids[b]) {
                        var c = v(a._rhaboo.storage);
                        m(a, b, c), o(a, a._rhaboo.kids[b].prev)
                    }
                }

                function r(a, b, c, d) {
                    if (void 0 !== a._rhaboo && void 0 !== a._rhaboo.slotnum) a._rhaboo.refs++;
                    else {
                        void 0 === a._rhaboo && Object.defineProperty(a, "_rhaboo", {
                            value: {},
                            writable: !0,
                            configurable: !0,
                            enumerable: !1
                        }), void 0 !== b && (a._rhaboo.storage = b), a._rhaboo.slotnum = void 0 !== c ? c : v(a._rhaboo.storage), a._rhaboo.refs = void 0 !== d ? d : 1, a._rhaboo.kids = {}, o(a);
                        for (var e in a) a.hasOwnProperty(e) && "_rhaboo" !== e && s(a, e)
                    }
                    return a
                }

                function s(a, b) {
                    q(a, b), "object" === B.typeOf(a[b]) && (void 0 === a[b]._rhaboo && Object.defineProperty(a[b], "_rhaboo", {
                        value: {
                            storage: a._rhaboo.storage
                        },
                        writable: !0,
                        configurable: !0,
                        enumerable: !1
                    }), r(a[b])), o(a, b)
                }

                function t(a, b) {
                    var c, d;
                    if (a._rhaboo.refs--, b || 0 === a._rhaboo.refs) {
                        for (d = void 0, c = a._rhaboo; c; c = a._rhaboo.kids[d = c.next]) a._rhaboo.storage.removeItem(z + c.slotnum), void 0 !== d && "object" == B.typeOf(a[d]) && t(a[d]);
                        delete a._rhaboo
                    }
                }

                function u(a, b) {
                    var c = a._rhaboo.kids[b];
                    if (void 0 !== c) {
                        var d = c.prev;
                        a._rhaboo.storage.removeItem(z + c.slotnum), "object" == B.typeOf(a[b]) && t(a[b]), n(a, b), o(a, d)
                    }
                }

                function v(a) {
                    var b = x && a === localStorage ? 0 : 1,
                        c = J[b];
                    return J[b]++, a.setItem(I, J[b]), c
                }
                void 0 === Function.prototype.name && void 0 !== Object.defineProperty && Object.defineProperty(Function.prototype, "name", {
                    get: function () {
                        var a = /function\s([^(]{1,})\(/,
                            b = a.exec(this.toString());
                        return b && b.length > 1 ? b[1].trim() : ""
                    },
                    set: function (a) { }
                });
                var w = !1,
                    x = e("localStorage"),
                    y = e("sessionStorage"),
                    z = "_rhaboo_",
                    A = {}, B = a("parunpar"),
                    C = B.sepByEsc("=", ":"),
                    D = C([B.string_pp, B.number_pp]),
                    E = B.pipe(function (a) {
                        return function (b) {
                            return a ? "Date" === b.constructor.name ? [b.constructor.name, b.toString()] : void 0 !== b.length ? [b.constructor.name, b.length.toString()] : [b.constructor.name] : new c[b[0]]("Date" == b[0] ? b[1] : b[1] ? Number(b[1]) : void 0)
                        }
                    })(C([B.string_pp, B.string_pp])),
                    F = function (a) {
                        return B.pipe(function (b) {
                            return function (c) {
                                return b ? B.runSnd({
                                    string: ["$", B.id],
                                    number: ["#", String],
                                    "boolean": ["?",
                                        function (a) {
                                            return a ? "t" : "f"
                                        }
                                    ],
                                    "null": ["~"],
                                    undefined: ["_"],
                                    object: ["&",
                                        function (a) {
                                            return a._rhaboo.slotnum
                                        }
                                    ]
                                }[B.typeOf(c)])(c) : {
                                    $: B.id,
                                    "#": Number,
                                    "?": B.eq("t"),
                                    "~": B.konst(null),
                                    _: B.konst(void 0),
                                    "&": k(a)
                                }[c[0]](c[1])
                            }
                        })(B.fixedWidth([1])([B.string_pp, B.string_pp]))
                    }, G = B.tuple([E, D]),
                    H = function (a) {
                        return B.tuple([F(a), D])
                    };
                Object.defineProperty(Object.prototype, "write", {
                    value: function (a, b) {
                        return q(this, a), "object" === B.typeOf(this[a]) && t(this[a]), this[a] = b, "object" === B.typeOf(b) && (void 0 === b._rhaboo && Object.defineProperty(b, "_rhaboo", {
                            value: {
                                storage: this._rhaboo.storage
                            },
                            writable: !0,
                            configurable: !0,
                            enumerable: !1
                        }), r(b)), o(this, a), this
                    }
                }), Object.defineProperty(Object.prototype, "erase", {
                    value: function (a) {
                        if (!this.hasOwnProperty(a)) return this;
                        "object" === B.typeOf(this[a]) && t(this[a]);
                        var b = this._rhaboo.kids[a];
                        this._rhaboo.storage.removeItem(z + b.slotnum);
                        var c = b.prev;
                        return n(this, a), o(this, c), delete this[a], this
                    }
                });
                for (var I = "_RHABOO_NEXT_SLOT", J = [0, 0], K = 0; 2 > K; K++) J[K] = x && localStorage.getItem(I) || 0, J[K] = Number(J[K]);
                b.exports = {
                    compatibilityMode: d,
                    persistent: g,
                    perishable: h,
                    inMemory: i,
                    construct: j,
                    addRef: r,
                    release: t,
                    storeProp: s,
                    forgetProp: u,
                    updateSlot: o
                }
            }).call(this, "undefined" != typeof global ? global : "undefined" != typeof self ? self : "undefined" != typeof window ? window : {})
        }, {
            parunpar: 1
        }
    ],
    4: [
        function (a, b, c) {
            (function (b) {
                b.Rhaboo = a("./arr")
            }).call(this, "undefined" != typeof global ? global : "undefined" != typeof self ? self : "undefined" != typeof window ? window : {})
        }, {
            "./arr": 2
        }
    ]
}, {}, [4]);